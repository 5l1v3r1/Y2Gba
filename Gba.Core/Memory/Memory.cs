﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{ 
    public class Memory : IArmMemoryReaderWriter
    {
        GameboyAdvance gba;

        byte[] ioReg = new byte[1024];

		// External Work Ram
		// Memory transfers to and from EWRAM are 16 bits wide and thus consume more cycles than necessary for 32 bit accesses
		byte[] EWRam = new byte[1024 * 256];

		// Internal Work Ram
		byte[] IWRam = new byte[1024 * 32];

		// Palette Ram
		byte[] PaletteRam = new byte[1024];


		public Memory(GameboyAdvance gba)
        {
            this.gba = gba;
        }


        public byte ReadByte(UInt32 address)
        {
			// ROM
			if (address >= 0x08000000 && address <= 0x09FFFFFF)
			{
				return gba.Rom.ReadByte(address - 0x08000000);
			}
			else if (address >= 0x04000000 && address <= 0x040003FE)
			{
				// (REG_IME) Turns all interrupts on or off
				if (address == 0x04000208)
				{
					return 0;
				}
				else
				{
					return ioReg[address - 0x04000000];
				}
			}
			// Fast Cpu linked RAM
			else if (address >= 0x03000000 && address <= 0x03007FFF)
			{
				return IWRam[address - 0x03000000];
			}
			// RAM
			else if (address >= 0x02000000 && address <= 0x0203FFFF)
			{
				return EWRam[address - 0x02000000];
			}
			// Palette Ram
			else if (address >= 0x05000000 && address <= 0x050003FF)
			{
				return PaletteRam[address - 0x05000000];
			}
			// VRam
			else if (address >= 0x06000000 && address <= 0x06017FFF)
			{
				throw new NotImplementedException();
			}
			// OAM Ram
			else if (address >= 0x07000000 && address <= 0x07FFFFFF)
			{
				throw new NotImplementedException();
			}
			else
			{
				throw new ArgumentException("Bad Memory Read");
			}
        }


        public ushort ReadHalfWord(UInt32 address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        public UInt32 ReadWord(UInt32 address)
        {
            // NB: Little Endian
            return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        public void WriteByte(UInt32 address, byte value)
        {
            if(address >= 0x04000000 && address <= 0x040003FE)
            {
				// (REG_IME) Turns all interrupts on or off
				if (address == 0x04000208)
				{

				}			
				else
				{
					ioReg[address - 0x04000000] = value;
				}
            }
			// Fast Cpu linked RAM
			else if (address >= 0x03000000 && address <= 0x03007FFF)
			{
				IWRam[address - 0x03000000] = value;
			}
			// RAM
			else if (address >= 0x02000000 && address <= 0x0203FFFF)
			{
				EWRam[address - 0x02000000] = value;
			}
			// Palette Ram
			else if (address >= 0x05000000 && address <= 0x050003FF)
			{
				PaletteRam[address - 0x05000000] = value;
			}
			// VRam
			else if (address >= 0x06000000 && address <= 0x06017FFF)
			{
				throw new NotImplementedException();
			}
			// OAM Ram
			else if (address >= 0x07000000 && address <= 0x07FFFFFF)
			{
				throw new NotImplementedException();
			}
			else
            {
				throw new ArgumentException("Bad Memory Write");
			}
        }


        public void WriteHalfWord(UInt32 address, ushort value)
        {
            WriteByte(address, (byte)(value & 0x00ff));
            WriteByte((address + 1), (byte)((value & 0xff00) >> 8));
        }


        public void WriteWord(UInt32 address, UInt32 value)
        {
            WriteByte(address, (byte)(value & 0x00ff));
            WriteByte((address + 1), (byte)((value & 0xff00) >> 8));
            WriteByte((address + 2), (byte)((value & 0xff0000) >> 16));
            WriteByte((address + 3), (byte)((value & 0xff000000) >> 24));
        }



		/****** Checks address before 32-bit reading/writing for special case scenarios ******/
		public void ReadWriteWord_Checked(UInt32 addr, ref UInt32 value, bool load_store)
		{
			//Assume normal operation until a special case occurs
			bool normal_operation = true;

			//Check for special case scenarios for read ops
			if (load_store)
			{
				//Misaligned LDR or SWP
				if (((addr & 0x1)!=0) || ((addr & 0x2)!= 0))
				{
					normal_operation = false;

					//Force alignment by word, then rotate right the read
					byte offset = (byte) ((addr & 0x3) * 8);
					value = ReadWord((UInt32) (addr & ~0x3));
					gba.Cpu.RotateRight(ref value, offset);
				}

				//Out of bounds unused memory
				if ((addr & ~0x3) >= 0x10000000)
				{
					normal_operation = false;

					//Read the opcode instruction at PC
					if (gba.Cpu.State == Cpu.CpuState.Arm) { value = ReadWord(gba.Cpu.R15); }
					else { value = (UInt32)(ReadHalfWord(gba.Cpu.R15) << 16) | ReadHalfWord(gba.Cpu.R15); }
				}

				//Return specific values when trying to read BIOS when PC is not within the BIOS
				if (((addr & ~0x3) <= 0x3FFF) && (gba.Cpu.R15 > 0x3FFF))
				{
					/*
					normal_operation = false;

					switch (bios_read_state)
					{
						case BIOS_STARTUP: value = 0xE129F000; break;
						case BIOS_IRQ_EXECUTE: value = 0xE25EF004; break;
						case BIOS_IRQ_FINISH: value = 0xE55EC002; break;
						case BIOS_SWI_FINISH: value = 0xE3A02004; break;
					}
					*/
				}

				//Special reads to I/O with some bits being unreadable
				switch (addr)
				{
					//Return only the readable halfword for the following addresses

					//DMAxCNT_L
					case 0x40000B8:
					case 0x40000C4:
					case 0x40000D0:
					case 0x40000DC:
						//value = (mem->read_u16_fast(addr + 2) << 16);
						value = (ushort) ((ReadHalfWord(addr + 2) << 16));
						normal_operation = false;
						break;
				}

				//Normal operation
				if (normal_operation) { value = ReadWord(addr); }
			}

			//Check for special case scenarios for write ops
			else
			{
				//Misaligned STR
				if ( ((addr & 0x1)!=0) || ((addr & 0x2)!=0))
				{
					normal_operation = false;

					//Force alignment by word, but that's all, no rotation
					WriteWord((UInt32)(addr & ~0x3), value);
				}

				//Normal operation
				else { WriteWord(addr, value); }
			}
		}
	}
}
