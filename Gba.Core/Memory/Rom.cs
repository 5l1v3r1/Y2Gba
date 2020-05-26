﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gba.Core
{
    /*
     https://mgba-emu.github.io/gbatek/#gbacartridgeheader
     Address Bytes Expl.
      000h    4     ROM Entry Point  (32bit ARM branch opcode, eg. "B rom_start")
      004h    156   Nintendo Logo    (compressed bitmap, required!)
      0A0h    12    Game Title       (uppercase ascii, max 12 characters)
      0ACh    4     Game Code        (uppercase ascii, 4 characters)
      0B0h    2     Maker Code       (uppercase ascii, 2 characters)
      0B2h    1     Fixed value      (must be 96h, required!)
      0B3h    1     Main unit code   (00h for current GBA models)
      0B4h    1     Device type      (usually 00h) (bit7=DACS/debug related)
      0B5h    7     Reserved Area    (should be zero filled)
      0BCh    1     Software version (usually 00h)
      0BDh    1     Complement check (header checksum, required!)
      0BEh    2     Reserved Area    (should be zero filled)
      --- Additional Multiboot Header Entries ---
      0C0h    4     RAM Entry Point  (32bit ARM branch opcode, eg. "B ram_start")
      0C4h    1     Boot mode        (init as 00h - BIOS overwrites this value!)
      0C5h    1     Slave ID Number  (init as 00h - BIOS overwrites this value!)
      0C6h    26    Not used         (seems to be unused)
      0E0h    4     JOYBUS Entry Pt. (32bit ARM branch opcode, eg. "B joy_start")
  */
    public class Rom : IRom
    {
        private byte[] romData;

        //private readonly int Header_Size = 0xC0;
        private readonly int RomNameOffset = 0x0A0;


        public string RomName { get; private set; }


        public string RomFileName { get; private set; }

        public UInt32 EntryPoint { get; private set; }

        public Rom(string fn)
        {
            RomFileName = fn;

            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();

            RomName = Encoding.UTF8.GetString(romData, RomNameOffset, 12).TrimEnd((Char)0);

            EntryPoint = ReadWord(0); // BitConverter.ToUInt32(romData, 0);

        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(UInt32 address)
        {
            return romData[address];            
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadHalfWord(UInt32 address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((UInt32)(address+1)) << 8) | ReadByte(address));
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 ReadWord(UInt32 address)
        {
            // NB: Little Endian
            return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }
    }
}