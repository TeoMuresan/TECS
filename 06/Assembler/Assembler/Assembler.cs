using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    /// <summary>
    /// Basic assembler for the Hack platform.
    /// </summary>
    public class Assembler
    {
        // Variables in the Hack platform are stored from address 16 upwards.
        private const int BaseAddressVar = 16;

        /// <summary>
        /// Gets or sets the input file path.
        /// </summary>
        /// <value> The input file path.</value>
        private string AsmFilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Dictionary type structure that holds all encountered symbols in the asm
        /// file and their line number +1 
        /// </summary>
        /// <value> The symbol table.</value>
        private SymbolTable SymbolTable
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Assembler"/> class.
        /// </summary>
        /// <param name="input"> The input.</param>
        public Assembler(string asmFilePath)
        {
            this.AsmFilePath = asmFilePath;
            this.SymbolTable = new SymbolTable();
        }

        /// <summary>
        /// Performs the full assembly process on the assembly input file.
        /// </summary>
        /// <returns> The binary code as a string of 0s and 1s.</returns>
        public string Assemble()
        {
            string hackCode = string.Empty;
            //First, remove junk from .asm file.
            byte[] cleanAsmCode = this.RemoveJunk(this.AsmFilePath);
            FirstPass(cleanAsmCode);
            hackCode = SecondPass(cleanAsmCode);

            return hackCode;
        }

        /// <summary>
        /// First pass of the assembly file.
        /// Go through the asm program, line by line, and
        /// add all the program’s labels, along with their ROM addresses, into the symbol table.
        /// The ROM addresses are consecutive numbers, starting at address 0.
        /// This method is designed to process junk-free asm files.
        /// The hack file won't contain labels since they are pseudo-commands and therefore not translated.
        /// So, the hack file will have (number of lines in asm file - number of labels) lines.
        /// </summary>
        /// <param name="inputBytes"> The input bytes.</param>
        private void FirstPass(byte[] inputBytes)
        {
            using (MemoryStream asmStream = new MemoryStream(inputBytes))
            {
                using (Parser parser = new Parser(asmStream))
                {
                    int ROMAddress = 0;
                    // Process .asm file line by line.
                    while (parser.HasMoreCommands())
                    {
                        parser.Advance();
                        if (parser.CommandType() == Command_Type.L_COMMAND)
                        {
                            // Store the label along with the corresponding program line in the hack file
                            // for the next instruction in the asm file.
                            this.SymbolTable.AddEntry(parser.Symbol(), ROMAddress);
                        }
                        else if (parser.CommandType() == Command_Type.C_COMMAND
                        || parser.CommandType() == Command_Type.A_COMMAND)
                        {
                            // Ignore non-label commands in the asm file.
                            ROMAddress++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Second pass of the assembly file.
        /// Go through the asm program, line by line, and
        /// complete the symbol table with the user-defined variables, along with their RAM addresses,
        /// and translate each line to binary code. 
        /// The RAM addresses are consecutive numbers, starting at address 16.
        /// This method is designed to be called after FirstPass().
        /// </summary>
        /// <param name="inputBytes"> The input bytes.</param>
        /// <returns> String representing the binary translation of the asm code provided as the input.</returns>
        private string SecondPass(byte[] inputBytes)
        {
            StringBuilder binaryCode = new StringBuilder();
            int addressCounter = BaseAddressVar;

            using (MemoryStream asmStream = new MemoryStream(inputBytes))
            {
                using (Parser parser = new Parser(asmStream))
                {
                    // Process .asm file line by line.
                    while (parser.HasMoreCommands())
                    {
                        String binaryLine = String.Empty;
                        parser.Advance();
                        // Based on its type, translate assembly command into a single 16-bit word.
                        if (parser.CommandType() == Command_Type.A_COMMAND)
                        {
                            binaryLine = HandleACommand(parser.Symbol(), ref addressCounter);
                        }
                        else if (parser.CommandType() == Command_Type.C_COMMAND)
                        {
                            binaryLine = Code.AssembleCInstruction(parser.Comp(), parser.Dest(), parser.Jump());
                        }
                        if (!string.IsNullOrEmpty(binaryLine))
                        {
                            binaryCode.Append(binaryLine + Environment.NewLine);
                        }
                    }
                }
            }

            return binaryCode.ToString();
        }

        /// <summary>
        /// Translates the A-command @symbol to binary code, where
        /// symbol is either a non-negative integer or a user-defined variable.        
        /// </summary>
        /// <param name="symbol"> The symbol from an A-command.</param>
        /// <param name="addressCounter"> The RAM address count.</param>
        /// <returns> String representing the binary code of the A-command.</returns>
        private string HandleACommand(string symbol, ref int addressCounter)
        {
            string binaryLine = string.Empty;

            int value;
            bool result = Int32.TryParse(symbol, out value);
            // If symbol is a valid number, translate the A-command.
            if (result)
            {
                binaryLine = Code.AssembleAInstruction(symbol);
            }               
            else
            {
                // If symbol is a label, get its corresponding ROM address and translate the A-command.
                if (SymbolTable.Contains(symbol))
                {
                    int address = SymbolTable.GetAddress(symbol);
                    binaryLine = Code.AssembleAInstruction(address.ToString());
                }
                else
                {
                    // If symbol is a user-defined variable, add it to the symbol table, along with a 
                    // RAM address and translate the A-command.
                    SymbolTable.AddEntry(symbol, addressCounter);
                    binaryLine = Code.AssembleAInstruction(addressCounter.ToString());
                    addressCounter++;
                }
            }

            return binaryLine;
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
