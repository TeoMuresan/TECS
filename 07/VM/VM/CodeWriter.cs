using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM
{
    /// <summary>
    /// Translates VM commands into Hack assembly code.
    /// Implements the singleton design pattern because it should be called only once for all vm files.    
    /// Creates a list containing the translation of all the vm files as asm code lines.
    /// </summary>
    public sealed class CodeWriter
    {
        public static readonly CodeWriter _instance = new CodeWriter();

        private const string stackBaseAddress = "256";
        private const string sysInitFunction = "Sys.init";
        /// <summary>
        /// The name of the VM function that is currently executing.
        /// </summary>
        private string currentExecutingFunction;// = sysInitFunction + "$";

        private string vmFileName;
        private List<string> linesOfAsmCode = new List<string>();
        // Eq, Gt and Lt are associated with "-" because the asm code that implements these commands
        // uses JEQ, JGT and JLT respectively, which perform the comparison with 0.
        // So the difference of the operands is required.
        private Dictionary<Arithmetic_Command, string> arithmeticCommandsSymbols = new Dictionary<Arithmetic_Command, string>
            {
                { Arithmetic_Command.Add, "+" }, { Arithmetic_Command.Sub, "-" }, { Arithmetic_Command.Neg, "-" },                
                { Arithmetic_Command.Eq, "-" }, { Arithmetic_Command.Gt, "-" }, { Arithmetic_Command.Lt, "-" },
                { Arithmetic_Command.And, "&" }, { Arithmetic_Command.Or, "|" }, { Arithmetic_Command.Not, "!" }
            };
        private Dictionary<VM_Segment, int> VMSegmentsRAM = new Dictionary<VM_Segment, int>
            {
                { VM_Segment.Local, 1 }, { VM_Segment.Argument, 2 }, { VM_Segment.This, 3 }, { VM_Segment.That, 4 },
                { VM_Segment.Pointer, 3 }, { VM_Segment.Temp, 5 }, { VM_Segment.Constant, -1 }, { VM_Segment.Static, -1 },
                // The following are not VM segments, but they're stored here for ease of reference.
                { VM_Segment.R13, 13 }, { VM_Segment.R14, 14 }, { VM_Segment.R15, 15 }
            };

        // The jump logic for the arithmetic commands gt, lt and eq requires labels.
        // Labels need to be unique, so keep counters.        
        private Dictionary<Arithmetic_Command, int> jumpLabelsCount = new Dictionary<Arithmetic_Command, int>
            {
                { Arithmetic_Command.Gt, 0 }, { Arithmetic_Command.Lt, 0 }, { Arithmetic_Command.Eq, 0 }
            };

        public string VmFileName
        {
            get { return this.vmFileName; }
            set { this.vmFileName = value; }
        }

        public List<string> LinesOfAsmCode
        {
            get { return this.linesOfAsmCode; }
            set { this.linesOfAsmCode = value; }
        }        

        #region ArithmeticCommands

        /// <summary>
        /// Writes the assembly code that is the translation of the given arithmetic command.
        /// </summary>
        /// <param name="command"> The arithmetic command to be translated.</param>
        public void WriteArithmetic(string command)
        {
            try
            {
                Arithmetic_Command aCommand = Command.arithmeticCommands[command];

                string opSymbol = arithmeticCommandsSymbols[aCommand];

                switch (aCommand)
                {
                    case Arithmetic_Command.Add:
                    case Arithmetic_Command.Sub:
                    case Arithmetic_Command.And:
                    case Arithmetic_Command.Or:
                        {
                            WriteGetOperandsAndCompute(opSymbol);
                            break;
                        }
                    case Arithmetic_Command.Neg:
                    case Arithmetic_Command.Not:
                        {
                            // The code in this section assumes there is at least 1 element in the stack.
                            WritePopStackToD();
                            linesOfAsmCode.Add(@"D=" + opSymbol + "D");
                            break;
                        }
                    case Arithmetic_Command.Gt:
                    case Arithmetic_Command.Lt:
                    case Arithmetic_Command.Eq:
                        {
                            WriteGetOperandsAndCompute(opSymbol);
                            WriteJumpLogic(command, jumpLabelsCount[aCommand]++);
                            break;
                        }
                }

                WritePushDToStack();
            }
            catch(Exception)
            {
                throw new Exception("WriteArithmetic should be called only for C_ARITHMETIC commands.");
            }
        }

        /// <summary>
        /// Writes assembly code that gets the 2 elements from stack top and applies the appropriate operation on them.
        /// This method assumes there are at least 2 elements in the stack.
        /// opSymbol can be:
        /// +, -, &, |, corresponding to add, sub, and, or respectively.
        /// For gt, lt, eq: the assembly jump commands compare numbers to 0, so the difference of
        ///                   the operands is required.
        /// </summary>
        /// <param name="opSymbol"> The operation symbol.</param>
        private void WriteGetOperandsAndCompute(string opSymbol)
        {
            int regAddress = VMSegmentsRAM[VM_Segment.R14];            

            WritePopStackToR(VM_Segment.R14);
            WritePopStackToD();
            linesOfAsmCode.Add(@"@" + regAddress);
            linesOfAsmCode.Add(@"D=D" + opSymbol + "M");
        }

        /// <summary>
        /// Writes assembly code that implements the jump logic.
        /// command is gt, lt or eq.
        /// </summary>
        /// <param name="command"> The arithmetic command.</param>
        private void WriteJumpLogic(string command, int labelCounter)
        {
            string commandUpper = command.ToUpper();
            string jumpLabelString = commandUpper + labelCounter;
            string jumpEndLabelString = commandUpper + "_END" + labelCounter;

            linesOfAsmCode.Add(@"@" + jumpLabelString);
            // The assembly jump command is JGT, JLT or JEQ.
            // Jump to label if the comparison with 0 succeeds.
            linesOfAsmCode.Add(@"D;J" + commandUpper);
            // 0 represents false.
            linesOfAsmCode.Add(@"D=0");
            linesOfAsmCode.Add(@"@" + jumpEndLabelString);
            linesOfAsmCode.Add(@"0;JMP");

            linesOfAsmCode.Add(@"(" + jumpLabelString + ")");
            // -1 represents true.
            linesOfAsmCode.Add(@"D=-1");
            linesOfAsmCode.Add(@"(" + jumpEndLabelString + ")");            
        }

        #endregion

        #region StackCommands

        /// <summary>
        /// Writes the assembly code that is the translation of the given command.
        /// Command is either C_PUSH or C_POP.
        /// C_POP commands on the 'constant' VM segment are ignored.
        /// </summary>
        /// <param name="command"> The command (C_PUSH or C_POP) to be translated.</param>
        /// <param name="segment"> The VM segment to push from or pop into.</param>
        /// <param name="index"> The address in the VM segment to push from or pop into.</param>
        public void WritePushPop(Command_Type command, string segment, int index)
        {
            VM_Segment vmSegment = Command.VMSegments[segment];

            if (command == Command_Type.C_PUSH)
            {
                WritePushStack(vmSegment, index);
            }
            else if (command == Command_Type.C_POP)
            {
                if (vmSegment != VM_Segment.Constant)
                {
                    WritePopStack(vmSegment, index);
                }
                else
                {
                    linesOfAsmCode.Add(@"//" + command + " commands on the '" + segment + "' VM segment are ignored.");
                }
            }
            else
            {
                throw new Exception("WritePushPop should be called only for C_PUSH or C_POP commands.");
            }
        }

        /// <summary>
        /// Writes assembly code that pops the stack into the specified VM segment index.
        /// </summary>
        /// <param name="segment"> The VM segment.</param>
        /// <param name="index"> The index of the VM segment.</param>
        private void WritePopStack(VM_Segment segment, int index)
        {
            List<string> codeForSettingAddress = WriteGetSegmentIndexAddress(segment, index);
            WritePopStackToD();
            foreach (string codeLine in codeForSettingAddress)
            {
                linesOfAsmCode.Add(codeLine);
            }
            // Store the stack top element, which is in D now, in the specified VM segment index.
            linesOfAsmCode.Add(@"M=D");
        }

        /// <summary>
        /// Writes assembly code that pushes the stack with the data in the specified VM segment index.
        /// </summary>
        /// <param name="segment"> The VM segment.</param>
        /// <param name="index"> The index of the VM segment.</param>
        private void WritePushStack(VM_Segment segment, int index)
        {
            List<string> codeForSettingAddress = WriteGetSegmentIndexAddress(segment, index);
            foreach (string codeLine in codeForSettingAddress)
            {
                linesOfAsmCode.Add(codeLine);
            }
            // The constant is a truly virtual memory segment so index is the constant itself and 
            // not an address pointing to the constant.
            if (segment == VM_Segment.Constant)
            {                
                linesOfAsmCode.Add(@"D=A");
            }
            else
            {
                // Store the specified VM segment index data in D.
                linesOfAsmCode.Add(@"D=M");
            }
            WritePushDToStack();
        }

        /// <summary>
        /// Writes assembly code that gets the address corresponding to the specified segment index.
        /// In case of Local, Argument, This, That, it stores the address, corresponding to the segment index, in R13.
        /// </summary>
        /// <param name="segment"> The VM segment to write into.</param>
        /// <param name="index"> The index of a specific address in the VM segment.</param>
        private List<string> WriteGetSegmentIndexAddress(VM_Segment segment, int index)            
        {
            int address = VMSegmentsRAM[segment];
            List<string> codeForSettingAddress = new List<string>();

            switch (segment)
            {
                case VM_Segment.Local:
                case VM_Segment.Argument:
                case VM_Segment.This:
                case VM_Segment.That:
                    {
                        WriteStoreSegmentPlusIndexPointerInR13(address, index);
                        codeForSettingAddress.Add(@"@R13");
                        codeForSettingAddress.Add(@"A=M");
                        break;
                    }
                case VM_Segment.Pointer:
                case VM_Segment.Temp:
                    {
                        address = address + index;
                        codeForSettingAddress.Add(@"@" + address);
                        break;
                    }
                case VM_Segment.Static:
                    {                        
                        codeForSettingAddress.Add(@"@" + VmFileName + "." + index);
                        break;
                    }
                case VM_Segment.Constant:
                    {
                        codeForSettingAddress.Add(@"@" + index);
                        break;
                    }
            }
            return codeForSettingAddress;
        }

        /// <summary>
        /// Writes assembly code that pops the stack into the specified register.       
        /// </summary>
        /// <param name="register"> The register.</param>
        private void WritePopStackToR(VM_Segment register)
        {
            int address = VMSegmentsRAM[register];
            WritePopStackToD();
            linesOfAsmCode.Add(@"@" + address);
            linesOfAsmCode.Add(@"M=D");            
        }

        /// <summary>
        /// Writes assembly code that pushes the stack with the data in the specified register.
        /// </summary>
        /// <param name="register"> The register.</param>
        private void WritePushRToStack(VM_Segment register)
        {
            int address = VMSegmentsRAM[register];
            linesOfAsmCode.Add(@"@" + address);
            linesOfAsmCode.Add(@"D=M");
            WritePushDToStack();
        }

        /// <summary>        
        /// Writes assembly code that computes segment_base_address + index and stores resulting address in R13.
        /// </summary>
        /// <param name="segmentBase"> The base address of the VM segment.</param>
        /// <param name="index"> The address in the VM segment to be processed.</param>
        private void WriteStoreSegmentPlusIndexPointerInR13(int segmentBase, int index)
        {
            this.linesOfAsmCode.Add(@"@" + index);
            // D=index
            this.linesOfAsmCode.Add(@"D=A");

            this.linesOfAsmCode.Add(@"@" + segmentBase);
            // A=segmentBase + index
            this.linesOfAsmCode.Add(@"D=D+M");            
            // R13=A
            this.linesOfAsmCode.Add(@"@R13");
            this.linesOfAsmCode.Add(@"M=D");
        }

        /// <summary>
        /// Writes assembly code that stores the stack top element in the assembly-level D register. 
        /// </summary>
        private void WriteStoreStackTopInD()
        {
            linesOfAsmCode.Add(@"@SP");            
            linesOfAsmCode.Add(@"A=M");
            linesOfAsmCode.Add(@"D=M");
        }

        /// <summary>
        /// Writes assembly code that pops the stack and stores the element in the assembly-level D register. 
        /// </summary>
        private void WritePopStackToD()
        {
            WriteDecrementStackPointer();
            WriteStoreStackTopInD();
        }

        /// <summary>
        /// Writes assembly code that pushes the stack with the data in the assembly-level D register. 
        /// </summary>
        private void WritePushDToStack() 
        {
            WriteStoreDOnStackTop();
            WriteIncrementStackPointer();
        }

        /// <summary>
        /// Writes assembly code that stores the assembly-level D register data in the stack.
        /// </summary>
        private void WriteStoreDOnStackTop()
        {
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"A=M");
            linesOfAsmCode.Add(@"M=D");
        }

        /// <summary>
        /// Writes assembly code that increments the stack pointer.
        /// </summary>
        private void WriteIncrementStackPointer()
        {
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"M=M+1");
        }

        /// <summary>
        /// Writes assembly code that decrements the stack pointer.
        /// </summary>
        private void WriteDecrementStackPointer()
        {
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"M=M-1");
        }

        #endregion

        #region ProgramFlow

        /// <summary>
        /// Writes assembly code that effects the VM initialization, also called bootstrap code.
        /// This code must be placed at the beginning of the output file.
        /// </summary>
        public void WriteInit()
        {
            // Initialize the stack pointer to address 256.
            linesOfAsmCode.Add(@"@" + stackBaseAddress);
            linesOfAsmCode.Add(@"D=A");
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"M=D");
            // Setting the LCL, ARG, THIS and THAT point­ers to known illegal values helps identify when a pointer
            // is used before it is initial­ized.
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.Local]);
            linesOfAsmCode.Add(@"M=-1");
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.Argument]);
            linesOfAsmCode.Add(@"M=-1");
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.This]);
            linesOfAsmCode.Add(@"M=-1");
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.That]);
            linesOfAsmCode.Add(@"M=-1");

            // When testing VM Part I: Stack Arithmetic, comment the following code line.
            // Start executing (the translated code of) Sys.init.
            WriteCall(sysInitFunction, 0);
        }

        /// <summary>
        /// Writes assembly code that effects the C_LABEL command.
        /// Each "label b" command in a VM function f should generate a globally unique symbol "f$b",
        /// where "f" is the function name and "b" is the label symbol within the VM function’s code.
        /// </summary>
        /// <param name="label"> The label to be declared.</param>
        public void WriteLabel(string label)
        {
            linesOfAsmCode.Add(@"(" + currentExecutingFunction + label + ")");
        }

        /// <summary>
        /// Writes assembly code that effects the C_GOTO command.
        /// The full label specification (as explained in WriteLabel()) must be used.
        /// </summary>
        /// <param name="label"> The label for an unconditional branching.</param>
        public void WriteGoto(string label)
        {
            linesOfAsmCode.Add(@"@" + currentExecutingFunction + label);
            linesOfAsmCode.Add(@"0;JMP");
        }

        /// <summary>
        /// Writes assembly code that effects the C_IF command.
        /// The full label specification (as explained in WriteLabel()) must be used.
        /// </summary>
        /// <param name="label"> The label for a conditional branching.</param>
        public void WriteIf(string label)
        {
            WritePopStackToD();
            linesOfAsmCode.Add(@"@" + currentExecutingFunction + label);
            // Jump to label if the value in D is not 0.
            linesOfAsmCode.Add(@"D;JNE");
        }

        #endregion

        #region FunctionCalling

        /// <summary>
        /// Writes assembly code that effects the C_FUNCTION command.
        /// </summary>
        /// <param name="functionName"> The function name.</param>
        /// <param name="numArgs"> The function's number of local variables.</param>
        public void WriteFunction(string functionName, int numLocals)
        {
            // Declare a label for the function entry.
            linesOfAsmCode.Add(@"(" + functionName + ")");
            // Initialize all the local variables to 0.
            for (int i = 0; i < numLocals; i++)
            {
                WritePushStack(VM_Segment.Constant, 0);
            }
        }

        /// <summary>
        /// Writes assembly code that effects the C_CALL command.
        /// </summary>
        /// <param name="functionName"> The function name.</param>
        /// <param name="numArgs"> The function's number of arguments.</param>
        public void WriteCall(string functionName, int numArgs)
        {
            // Label for the return address i.e. the memory location (in the target platform’s memory) of the command 
            // following the function call.
            string returnAddressLabel = "RETURN" + Guid.NewGuid().ToString("N");

            // Store the return address in D and push it to stack.
            linesOfAsmCode.Add(@"@" + returnAddressLabel);
            linesOfAsmCode.Add(@"D=A");
            WritePushDToStack();

            // Save the segments of the calling function.
            WritePushRToStack(VM_Segment.Local);
            WritePushRToStack(VM_Segment.Argument);
            WritePushRToStack(VM_Segment.This);
            WritePushRToStack(VM_Segment.That);

            // Reposition the ARG base address to the first argument's address in the stack:
            // ARG = SP - numArgs - 5.
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"D=M");
            linesOfAsmCode.Add(@"@" + numArgs);
            linesOfAsmCode.Add("D=D-A");
            linesOfAsmCode.Add(@"@5");
            linesOfAsmCode.Add("D=D-A");
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.Argument]);
            linesOfAsmCode.Add(@"M=D");

            // Reposition the LCL base address to the stack pointer:
            // LCL = SP
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"D=M");
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.Local]);
            linesOfAsmCode.Add(@"M=D");

            // Transfer control.
            linesOfAsmCode.Add(@"@" + functionName);
            linesOfAsmCode.Add(@"0;JMP");

            // Declare a label for the return address.
            linesOfAsmCode.Add(@"(" + returnAddressLabel + ")");

            currentExecutingFunction = functionName + "$";
        }

        /// <summary>
        /// Writes assembly code that effects the C_RETURN command.
        /// </summary>
        public void WriteReturn()
        {
            string ARG = @"@" + VMSegmentsRAM[VM_Segment.Argument];                        
            // Temporary variables.
            string FRAME = @"@FRAME";
            string RET = @"@RET";

            // Put the LCL base address in a temp var.
            linesOfAsmCode.Add(@"@" + VMSegmentsRAM[VM_Segment.Local]);
            linesOfAsmCode.Add(@"D=M");
            linesOfAsmCode.Add(FRAME);
            linesOfAsmCode.Add(@"M=D");

            // Put the return address in a temp var.
            WriteStoreValueAtAddressBelowFRAMEInD(FRAME, 5);
            linesOfAsmCode.Add(RET);
            linesOfAsmCode.Add(@"M=D");

            // Reposition the return value for the caller:
            // *ARG = pop().
            WritePopStackToD();
            linesOfAsmCode.Add(ARG);
            linesOfAsmCode.Add(@"A=M");
            linesOfAsmCode.Add(@"M=D");

            // Restore SP of the caller:
            // SP = ARG+1.
            linesOfAsmCode.Add(ARG);
            linesOfAsmCode.Add(@"D=M+1");
            linesOfAsmCode.Add(@"@SP");
            linesOfAsmCode.Add(@"M=D");

            // Restore the segments of the calling function.
            WriteRestoreSegment(VM_Segment.That, FRAME, 1);
            WriteRestoreSegment(VM_Segment.This, FRAME, 2);
            WriteRestoreSegment(VM_Segment.Argument, FRAME, 3);
            WriteRestoreSegment(VM_Segment.Local, FRAME, 4);

            // Go to return address (in the caller’s code).
            linesOfAsmCode.Add(RET);
            linesOfAsmCode.Add(@"A=M");
            linesOfAsmCode.Add(@"0;JMP");
        }

        /// <summary>
        /// Writes assembly code that gets the value at the address which is frameOffset addresses below FRAME in the stack.
        /// Stores that value in D.
        /// </summary>
        /// <param name="currentFrameLabel"> The FRAME temp variable that is a copy of LCL.</param>
        /// <param name="frameOffset"> The number of elements to go, below FRAME, in the stack.</param>
        private void WriteStoreValueAtAddressBelowFRAMEInD(string currentFrameLabel, int frameOffset)
        {
            linesOfAsmCode.Add(currentFrameLabel);
            linesOfAsmCode.Add(@"D=M");
            linesOfAsmCode.Add(@"@" + frameOffset);
            linesOfAsmCode.Add(@"A=D-A");
            linesOfAsmCode.Add(@"D=M");
        }

        /// <summary>
        /// Writes assembly code that restores the segment of the calling function.
        /// segment = *(currentFrameLabel - frameOffset).
        /// </summary>
        /// <param name="segment"> The segment to be restored.</param>
        /// <param name="currentFrameLabel"> The FRAME temp variable that is a copy of LCL.</param>
        /// <param name="frameOffset"> The number of elements to go below FRAME in the stack.</param>
        private void WriteRestoreSegment(VM_Segment segment, string currentFrameLabel, int frameOffset)
        {
            int address = VMSegmentsRAM[segment];
            WriteStoreValueAtAddressBelowFRAMEInD(currentFrameLabel, frameOffset);
            linesOfAsmCode.Add(@"@" + address);
            linesOfAsmCode.Add(@"M=D");
        }

        #endregion

        /// <summary>
        /// Writes assembly code that implements an infinite loop which terminates the assembly program’s execution.
        /// Upon reaching this loop, the program remains stuck in it until 
        /// the number of ticktocks, set in the test script, is reached.
        /// </summary>
        public void WriteInfiniteLoopAtEnd()
        {
            string endLabel = "END_INFINITE_LOOP_" + Guid.NewGuid().ToString("N");
            linesOfAsmCode.Add(@"(" + endLabel + ")");
            linesOfAsmCode.Add(@"@" + endLabel);
            linesOfAsmCode.Add(@"0;JMP");
        }
    }
}
