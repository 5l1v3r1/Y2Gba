﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gba.Core;

namespace GbaDebugger
{
    public class ConditionalExpression
    {
        public enum EqualityCheck
        {
            Equal,
            NotEqual,
            GtEqual,
            LtEqual,

            Invalid
        }

        UInt32 lhs, rhs;
        EqualityCheck equalitycheck;

        IArmMemoryReaderWriter memory;


        public ConditionalExpression(IArmMemoryReaderWriter memory, UInt32 lhs, EqualityCheck op, UInt32 rhs)
        {
            this.memory = memory;
            this.lhs = lhs;
            this.rhs = rhs;
            this.equalitycheck = op;
        }

        public ConditionalExpression(IArmMemoryReaderWriter memory, string[] terms)
        {
            this.memory = memory;
            if (terms.Length != 4)
            {
                throw new ArgumentException("ConditionalExpression arguments wrong. Form must be 'if <x> <==> <y>");
            }

            if (terms[0].Equals("if", StringComparison.OrdinalIgnoreCase) == false) throw new ArgumentException("missing if");

            if (ParseU32Parameter(terms[1], out lhs) == false ||
                ParseU32Parameter(terms[3], out rhs) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: params incorrect");
            }

            if (ParseEqualityParameter(terms[2], out equalitycheck) == false)
            {
                throw new ArgumentException("ConditionalExpression arguments: Invalid equality check");
            }

        }


        public bool Evaluate()
        {
            switch (equalitycheck)
            {
                case EqualityCheck.Equal:
                    return (memory.ReadWord(lhs) == rhs);

                case EqualityCheck.NotEqual:
                    return (memory.ReadWord(lhs) != rhs);

                case EqualityCheck.GtEqual:
                    return (memory.ReadWord(lhs) >= rhs);

                case EqualityCheck.LtEqual:
                    return (memory.ReadWord(lhs) <= rhs);
            }
            return false;
        }


        // TODO: This should be comomn code 
        bool ParseU32Parameter(string p, out UInt32 value)
        {
            if (UInt32.TryParse(p, out value) == false)
            {
                // Is it hex?
                if (p.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    p = p.Substring(2);
                }
                return UInt32.TryParse(p, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out value);
            }
            return true;
        }


        bool ParseEqualityParameter(string p, out EqualityCheck value)
        {
            if (p.Equals("=="))
            {
                value = EqualityCheck.Equal;
                return true;
            }

            if (p.Equals("!="))
            {
                value = EqualityCheck.NotEqual;
                return true;
            }

            if (p.Equals(">="))
            {
                value = EqualityCheck.GtEqual;
                return true;
            }

            if (p.Equals("<="))
            {
                value = EqualityCheck.LtEqual;
                return true;
            }

            value = EqualityCheck.Invalid;
            return false;
        }


        public override string ToString()
        {
            return String.Format("{0} {1} {2}", lhs, equalitycheck.ToString(), rhs);
        }
    }
}