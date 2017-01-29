using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using VM;

namespace Compiler
{
    /// <summary>
    /// Emits VM commands into a file, using the VM command syntax.
    /// </summary>
    public class VMWriter : IDisposable
    {
        private static Dictionary<VM_Segment, string> VMSegments = new Dictionary<VM_Segment, string>
        {
            { VM_Segment.Local, "local" }, { VM_Segment.Argument, "argument" }, { VM_Segment.This, "this" },
            { VM_Segment.That, "that" }, { VM_Segment.Pointer, "pointer" }, { VM_Segment.Temp, "temp" },
            { VM_Segment.Static, "static" }, { VM_Segment.Constant, "constant" }
        };

        private static Dictionary<Command_Type, string> VMCommands = new Dictionary<Command_Type, string>
        {
            { Command_Type.C_PUSH, "push" }, { Command_Type.C_POP, "pop" },
            { Command_Type.C_LABEL, "label" }, { Command_Type.C_GOTO, "goto" }, { Command_Type.C_IF, "if-goto" },
            { Command_Type.C_CALL, "call" }, { Command_Type.C_FUNCTION, "function" }, { Command_Type.C_RETURN, "return" }
        };

        public static Dictionary<Arithmetic_Command, string> arithmeticCommands = new Dictionary<Arithmetic_Command, string> 
        { 
            { Arithmetic_Command.Add, "add" }, { Arithmetic_Command.Sub, "sub" }, { Arithmetic_Command.Neg, "neg" },
            { Arithmetic_Command.Eq, "eq" }, { Arithmetic_Command.Gt, "gt" }, { Arithmetic_Command.Lt, "lt" },
            { Arithmetic_Command.And, "and" }, { Arithmetic_Command.Or, "or" }, { Arithmetic_Command.Not, "not" }           
        };

        public static Dictionary<IdentifierCategory, VM_Segment> idenCatToVMSegment = new Dictionary<IdentifierCategory, VM_Segment>
        {
            { IdentifierCategory.VAR, VM_Segment.Local }, { IdentifierCategory.ARG, VM_Segment.Argument },
            { IdentifierCategory.STATIC, VM_Segment.Static }, { IdentifierCategory.FIELD, VM_Segment.This }
        };

        private StreamWriter writer;

        /// <summary>
        /// Prepares the stream given as parameter for writing.
        /// </summary>
        public VMWriter(MemoryStream vmStream)
        {            
            writer = new StreamWriter(vmStream);
        }

        /// <summary>
        /// Writes a VM push command.
        /// </summary>
        /// <param name="vmSegment"> The VM segment to push from.</param>
        /// <param name="index"> The address in the VM segment to push from.</param>
        public void WritePush(VM_Segment vmSegment, int index)
        {
            writer.WriteLine(VMCommands[Command_Type.C_PUSH] + " " + VMSegments[vmSegment] + " " + index);
        }

        /// <summary>
        /// Writes a VM pop command.
        /// </summary>
        /// <param name="vmSegment"> The VM segment to pop into.</param>
        /// <param name="index"> The address in the VM segment to pop into.</param>
        public void WritePop(VM_Segment vmSegment, int index)
        {
            writer.WriteLine(VMCommands[Command_Type.C_POP] + " " + VMSegments[vmSegment] + " " + index);
        }

        /// <summary>
        /// Writes a VM arithmetic command.
        /// </summary>
        /// <param name="command"> The arithmetic command.</param>
        public void WriteArithmetic(Arithmetic_Command command)
        {
            writer.WriteLine(arithmeticCommands[command]);
        }

        /// <summary>
        /// Writes a VM label command.
        /// </summary>
        /// <param name="label"> The label to be declared.</param>
        public void WriteLabel(string label)
        {
            writer.WriteLine(VMCommands[Command_Type.C_LABEL] + " " + label);
        }

        /// <summary>
        /// Writes a VM goto command.
        /// </summary>
        /// <param name="label"> The label for an unconditional branching.</param>
        public void WriteGoto(string label)
        {
            writer.WriteLine(VMCommands[Command_Type.C_GOTO] + " " + label);
        }

        /// <summary>
        /// Writes a VM if-goto command.
        /// </summary>
        /// <param name="label"> The label for a conditional branching.</param>
        public void WriteIf(string label)
        {
            writer.WriteLine(VMCommands[Command_Type.C_IF] + " " + label);
        }

        /// <summary>
        /// Writes a VM call command.
        /// </summary>
        /// <param name="name"> The function name.</param>
        /// <param name="nArgs"> The function's number of arguments.</param>
        public void WriteCall(string name, int nArgs)
        {
            writer.WriteLine(VMCommands[Command_Type.C_CALL] + " " + name + " " + nArgs);
        }

        /// <summary>
        /// Writes a VM function command.
        /// </summary>
        /// <param name="name"> The function name.</param>
        /// <param name="numArgs"> The function's number of local variables.</param>
        public void WriteFunction(string name, int nLocals)
        {
            writer.WriteLine(VMCommands[Command_Type.C_FUNCTION] + " " + name + " " + nLocals);
        }

        /// <summary>
        /// Writes a VM return command.
        /// </summary>
        public void WriteReturn()
        {
            writer.WriteLine(VMCommands[Command_Type.C_RETURN]);
        }

        /// <summary>
        /// Necessary because the class implements IDisposable.
        /// </summary>
        public void Dispose()
        {
            if (writer != null)
            {
                writer.Flush();
            }
        }
    }
}
