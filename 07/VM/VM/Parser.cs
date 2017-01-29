using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM
{
    /// <summary>
    /// Handles the parsing of a single .vm file.
    /// Encapsulates access to the input code.
    /// Reads VM commands, parses them, and provides convenient access to their components.
    /// In addition, removes all white space and comments.
    /// </summary>
    public class Parser : IDisposable
    {
        private StreamReader reader;        
        public string currentLineCommand;

        /// <summary>
        /// Opens the input file/stream and gets ready to parse it.
        /// </summary>
        /// <param name="vmFileStream"> Stream of the vm file to be translated.</param>
        public Parser(Stream vmFileStream)
        {
            reader = new StreamReader(vmFileStream);
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
        /// Returns the type of the current VM command.
        /// Four main types of commands:
        /// Arithmetic commands perform arithmetic and logical operations on the stack: C_ARITHMETIC;
        /// Memory access commands transfer data between the stack and virtual memory segments: C_PUSH, C_POP;
        /// Program flow commands facilitate conditional and unconditional branching operations: C_LABEL, C_GOTO, 
        /// C_IF;
        /// Function calling commands call functions and return from them: C_FUNCTION, C_RETURN, C_CALL.
        /// </summary>
        /// <returns> The command type.</returns>
        public Command_Type CommandType()
        {
            Command_Type commandType = Command_Type.ERROR;            
            int i=0;
            // The following code is a more efficient way of implementing the structure:
            // if(!Command.ArithmeticCommand(currentLineCommand).Equals(Command_Type.ERROR))
            //   {return Command.ArithmeticCommand(currentLineCommand);}
            // else if(...)
            // ...
            do
            {
                commandType = Command.CommandTypeDelegates[i++](currentLineCommand);
            }
            while (commandType.Equals(Command_Type.ERROR) && i < Command.comTypeDelLength);

            return commandType;
        }

        /// <summary>
        /// Returns the first argument of the current command.
        /// In the case of C_ARITHMETIC, the command itself (add, sub, etc.) is returned.
        /// Should not be called if the current command is C_RETURN. 
        /// </summary>
        /// <returns> The first argument of the current command.</returns>
        public string Arg1()
        {
            string result = null;
            Command_Type commandType = this.CommandType();
            if (commandType == Command_Type.C_RETURN)
            {
                throw new Exception("Arg1 should not be called when current command is a return command.");
            }
            else if (commandType == Command_Type.C_ARITHMETIC)
            {
                result = currentLineCommand;
            }
            else
            {
                string[] wordArray = this.currentLineCommand.Split(' ');
                result = wordArray[1];
            }

            return result;
        }

        /// <summary>
        /// Returns the second argument of the current command.
        /// Should be called only if the current command is C_PUSH, C_POP, C_FUNCTION, or C_CALL.
        /// </summary>
        /// <returns> The second argument of the current command.</returns>
        public int Arg2()
        {
            int result = -1;
            Command_Type commandType = this.CommandType();
            if (commandType == Command_Type.C_PUSH || commandType == Command_Type.C_POP ||
                commandType == Command_Type.C_FUNCTION || commandType == Command_Type.C_CALL)
            {
                string[] wordArray = this.currentLineCommand.Split(' ');
                // CommandType matches the current command against a regex pattern that constrains the
                // second argument to be a non-negative integer. So there is no need to check this here.
                Int32.TryParse(wordArray[2], out result);
            }
            else
            {
                throw new Exception("Arg2 should be called when command type is: " + commandType + ".");
            }

            return result;
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
