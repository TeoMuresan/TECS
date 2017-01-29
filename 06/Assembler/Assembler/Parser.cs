using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    /// <summary>
    /// Encapsulates access to the input code. 
    /// Reads an assembly language command, parses it, and provides convenient access to 
    /// the command’s components (fields and symbols).
    /// In addition, removes all white space and comments.
    /// </summary>
    public class Parser : IDisposable
    {
        private StreamReader reader;
        public string currentLineCommand;

        /// <summary>
        /// Opens the input file/stream and gets ready to parse it.
        /// </summary>
        /// <param name="asmFileStream"> Stream of the asm file to be assembled.</param>
        public Parser(Stream asmFileStream)
        {
            reader = new StreamReader(asmFileStream);
        }

        /// <summary>
        /// Checks if there are more commands in the file.
        /// </summary>
        /// <returns> True, if there are more commands; false, otherwise.</returns>
        public bool HasMoreCommands()
        {
            return !reader.EndOfStream;
        }

        /// <summary>
        /// Reads the next command from the input and makes it the current command.
        /// Should be called only if hasMoreCommands() is true.
        /// Initially there is no current command.
        /// </summary>
        public void Advance()
        {
            if (HasMoreCommands())
            {
                currentLineCommand = reader.ReadLine();
            }
        }

        /// <summary>
        /// Returns the type of the current command:
        /// A_COMMAND for @Xxx where Xxx is either a symbol or a decimal unsigned int;
        /// C_COMMAND for dest=comp;jump;
        /// L_COMMAND (actually pseudo-command) for (Xxx) where Xxx is a symbol.
        /// </summary>
        /// <returns> The command type.</returns>
        public Command_Type CommandType()
        {            
            Command_Type commandType = Command_Type.ERROR;
            if (Command.IsA_COMMAND(currentLineCommand))
            {
                commandType = Command_Type.A_COMMAND;
            }
            else if (Command.IsC_COMMAND(currentLineCommand))
            {
                commandType = Command_Type.C_COMMAND;
            }
            else if (Command.IsL_COMMAND(currentLineCommand))
            {
                commandType = Command_Type.L_COMMAND;
            }

            return commandType;
        }

        /// <summary>
        /// Extracts the symbol or decimal Xxx of the current command @Xxx or (Xxx).
        /// Should be called only when commandType() is A_COMMAND or L_COMMAND.
        /// </summary>
        /// <returns> The symbol of the current command.</returns>
        public string Symbol()
        {
            if (this.CommandType() == Command_Type.C_COMMAND)
            {
                throw new Exception("Symbol should only be called when CommandType is A_COMMAND"
                    + " or L_COMMAND " + Environment.NewLine
                    + "Current command type: " + this.CommandType() + Environment.NewLine
                    + "Current command: " + currentLineCommand);

            }

            return Command.GetSymbol(currentLineCommand);
        }

        /// <summary>
        /// Returns the dest mnemonic in the current C-command (8 possibilities).
        /// Should be called only when commandType() is C_COMMAND.
        /// </summary>
        /// <returns> The dest mnemonic.</returns>
        public string Dest()
        {
            if (this.CommandType() != Command_Type.C_COMMAND)
            {
                throw new Exception("Dest should only be called when CommandType is C_COMMAND " + Environment.NewLine
                    + "Current command type: " + this.CommandType() + Environment.NewLine
                    + "Current command: " + currentLineCommand);

            }

            return Command.GetC_COMMANDFields(currentLineCommand).dest;
        }

        /// <summary>
        /// Returns the comp mnemonic in the current C-command (28 possibilities).
        /// Should be called only when commandType() is C_COMMAND.
        /// </summary>
        /// <returns> The comp mnemonic.</returns>
        public string Comp()
        {
            if (this.CommandType() != Command_Type.C_COMMAND)
            {
                throw new Exception("Comp should only be called when CommandType is C_COMMAND " + Environment.NewLine
                    + "Current command type: " + this.CommandType() + Environment.NewLine
                    + "Current command: " + currentLineCommand);

            }

            return Command.GetC_COMMANDFields(currentLineCommand).comp;
        }

        /// <summary>
        /// Returns the jump mnemonic in the current C-command (8 possibilities).
        /// Should be called only when commandType() is C_COMMAND.
        /// </summary>
        /// <returns> The comp mnemonic.</returns>
        public string Jump()
        {
            if (this.CommandType() != Command_Type.C_COMMAND)
            {
                throw new Exception("Jump should only be called when CommandType is C_COMMAND " + Environment.NewLine
                    + "Current command type: " + this.CommandType() + Environment.NewLine
                    + "Current command: " + currentLineCommand);

            }

            return Command.GetC_COMMANDFields(currentLineCommand).jump;
        }        

        /// <summary>
        /// Closes the stream reader.
        /// </summary>
        public void Dispose()
        {
            if (reader != null)
            {
                reader.Close();
            }
        }
    }
}
