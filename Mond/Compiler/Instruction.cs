﻿using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Compiler
{
    enum InstructionType : byte
    {
        Dup, Dup2, Drop, Swap, Swap1For2,   // duplicates, drop, swaps

        LdUndef, LdNull,                    // load undefined/null constant
        LdTrue, LdFalse,                    // load true/false constant
        LdNum, LdStr,                       // load number/string constant
        LdGlobal, LdGlobalFld,              // load global object, load field from global object

        LdLocF, StLocF,                     // load/store local in current frame
        LdArgF, StArgF,                     // load/store args in current frame
        LdFld, StFld,                       // load/store field
        LdArr, StArr,                       // load/store array
        LdArrF, StArrF,                     // load/store to static index of array in current frame
        LdUp, LdUpValue, StUpValue,         // load upvalue array, load/store upvalues (variables captured by closures)
        SeqResume, SeqSuspend,              // load/store current frame stack and evals for sequences

        NewObject, NewArray,                // create object/array
        Slice,                              // slice array

        Add, Sub, Mul, Div, Mod, Exp,       // math
        Neg,                                // negate
        Eq, Neq, Gt, Gte, Lt, Lte,          // comparison operators
        Not,                                // logical not

        In, NotIn,                          // contains, !contains

        BitLShift, BitRShift,               // bitwise shift
        BitAnd, BitOr, BitXor,              // bitwise and, bitwise or, bitwise xor
        BitNot,                             // bitwise not (one's compliment)

        Closure, Call, TailCall,            // make closure, call function, tail call
        Enter, Ret,                         // push locals/begin function, pop locals and return from function
        VarArgs,                            // setup variable length args

        Jmp,                                // jump unconditionally
        JmpTrueP, JmpFalseP,                // jump if peek() == true/false
        JmpTrue, JmpFalse,                  // jump if pop() == true/false
        JmpTable,                           // jump to one of multiple locations

        Breakpoint,                         // break if a debugger is attached
        DebugCheckpoint,                    // break if a debugger is attached and wants to break here

        InstanceCall,                       // call a field = x.y(z) -> x.y(x, z), but only evaluate x once

        IncF, DecF,                         // increment/decrement local in current frame

        // ----------------- //

        Label = 200,                        // label binding

        DebugInfo,                          // debug info (shouldn't be used)
        Function,                           // debug info
        Position,                           // debug info
        Statement,                          // debug info
        Scope,                              // debug info
    }

    internal sealed class Instruction
    {
        private int _offset;

        public InstructionType Type { get; }
        public ReadOnlyCollection<IInstructionOperand> Operands { get; }

        public Instruction(InstructionType type, params IInstructionOperand[] operands)
        {
            Type = type;
            Operands = new ReadOnlyCollection<IInstructionOperand>(operands);
        }

        public int Offset
        {
            get => _offset;
            set
            {
                if (Type == InstructionType.Label)
                {
                    var label = (LabelOperand)Operands[0];
                    label.Position = value;
                }

                _offset = value;
            }
        }

        public int Length
        {
            get
            {
                if (Type == InstructionType.Label ||
                    Type >= InstructionType.DebugInfo)
                {
                    return 0;
                }

                return Math.Max(Operands.Sum(o => o.Length), 1);
            }
        }

        public void Print()
        {
            if (Type == InstructionType.Label)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else if (Type >= InstructionType.DebugInfo)
                Console.ForegroundColor = ConsoleColor.DarkYellow;

            Console.Write("{0:X4} {1,-15} ", Offset, Type.ToString().ToLower());

            foreach (var operand in Operands)
            {
                operand.Print();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Write(BytecodeWriter writer)
        {
            if (Type == InstructionType.Label ||
                Type >= InstructionType.DebugInfo)
            {
                return;
            }

            var firstOperand = Operands.FirstOrDefault();
            writer.Write(Type, firstOperand?.FirstValue ?? 0);
            firstOperand?.Write(writer);

            foreach (var operand in Operands.Skip(1))
            {
                writer.Write(operand.FirstValue);
                operand.Write(writer);
            }
        }
    }
}
