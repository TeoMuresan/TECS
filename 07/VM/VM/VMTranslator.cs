using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM
{
    public class VMTranslator
    {
        /// <summary>
        /// Gets or sets a list containing the vm file(s) to be translated.
        /// </summary>
        /// <value> The vm file(s) to be translated</value>
        private List<string> VMFilesPaths
        {
            get;
            set;
        }

        public VMTranslator(List<string> vmFilesPaths)
        {
            VMFilesPaths = vmFilesPaths;
        }

        /// <summary>
        /// Translates the VM code in every input .vm file to assembly code.
        /// </summary>
        /// <returns> A list containing the translation of each line in each vm file to assembly code.</returns>
        public List<string> Translate()
        {
            CodeWriter._instance.WriteInit();

            foreach (string vmFilePath in VMFilesPaths)
            {
                CodeWriter._instance.VmFileName = Path.GetFileNameWithoutExtension(vmFilePath);
                byte[] cleanVmCodeBytes = this.RemoveJunk(vmFilePath);
                using (MemoryStream vmStream = new MemoryStream(cleanVmCodeBytes))
                {
                    using (Parser parser = new Parser(vmStream))
                    {
                        while (parser.HasMoreCommands())
                        {
                            parser.Advance();
                            Command_Type commandType = parser.CommandType();
                            
                            switch (commandType)
                            {
                                case Command_Type.C_PUSH:
                                case Command_Type.C_POP:
                                    {
                                        CodeWriter._instance.WritePushPop(commandType, parser.Arg1(), parser.Arg2());
                                        break;
                                    }
                                case Command_Type.C_ARITHMETIC:                                
                                    {
                                        CodeWriter._instance.WriteArithmetic(parser.Arg1());
                                        break;
                                    }
                                case Command_Type.C_LABEL:
                                    {
                                        CodeWriter._instance.WriteLabel(parser.Arg1());
                                        break;
                                    }
                                    case Command_Type.C_GOTO:
                                    {
                                        CodeWriter._instance.WriteGoto(parser.Arg1());
                                        break;
                                    }
                                case Command_Type.C_IF:                                
                                    {
                                        CodeWriter._instance.WriteIf(parser.Arg1());
                                        break;
                                    }
                                case Command_Type.C_FUNCTION:
                                    {
                                        CodeWriter._instance.WriteFunction(parser.Arg1(), parser.Arg2());
                                        break;
                                    }
                                case Command_Type.C_CALL:
                                    {
                                        CodeWriter._instance.WriteCall(parser.Arg1(), parser.Arg2());
                                        break;
                                    }
                                case Command_Type.C_RETURN:
                                    {
                                        CodeWriter._instance.WriteReturn();
                                        break;
                                    }
                                default:
                                    {
                                        //Command type is C_ERROR.                                
                                        throw new Exception("Command type for '" + parser.currentLineCommand + "' is invalid.");
                                    }
                            }
                        }
                    }
                }
            }

            // There is a finite number of ticktocks when executing an assembly program.
            // This instruction terminates the program’s execution by putting the computer in an infinite loop.
            CodeWriter._instance.WriteInfiniteLoopAtEnd();

            return CodeWriter._instance.LinesOfAsmCode;
        }

        /// <summary>
        /// Removes junk from the stream and returns a byte array
        /// i.e. removes comments etc.
        /// </summary>
        /// <returns></returns>
        private byte[] RemoveJunk(string inputFilePath)
        {
            byte[] result;

            using (FileStream fileStream = new FileStream(inputFilePath, FileMode.Open))
            {
                using (JunkRemover junkRemover = new JunkRemover(fileStream))
                {
                    result = junkRemover.RemoveJunk();
                }
            }

            return result;
        }
    }
}
