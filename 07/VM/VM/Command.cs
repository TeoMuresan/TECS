using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VM
{
    // The types of possible commands in the VM language
    // and an error command.
    public enum Command_Type
    {
        C_ARITHMETIC, C_PUSH, C_POP, C_LABEL, C_GOTO,
        C_IF, C_FUNCTION, C_RETURN, C_CALL, ERROR 
    };
    public enum Arithmetic_Command { Add, Sub, Neg, Eq, Gt, Lt, And, Or, Not, None };
    #region VMSegmentsExplained
    //
    // local: function’s local variables;
    // argument: function’s arguments;
    // this, that: general-purpose segments;c an be made to correspond to different areas in the heap;
    // pointer:  two-entry segment that holds the base addresses of the <this> and <that> segments;
    // temp: fixed eight-entry segment that holds temporary variables for general use;
    // static:  static variables shared by all functions in the same .vm file;
    // constant: pseudo-segment that holds all the constants in the range 0 ... 32767=(2^15)-1. 
    //
    //
    // local, argument, this, that:
    // - mapped directly on Hack RAM;
    // - physical base address stored in a dedicated register (LCL, ARG, THIS, and THAT, respectively);
    // - the i-th entry has the address base+i, where base is RAM[1], RAM[2], RAM[3] and RAM[4], respectively).
    // 
    // pointer, temp:
    // - mapped directly onto a fixed area in Hack RAM;
    // - pointer is mapped on RAM locations 3-4 (THIS, THAT): pointer[i]=RAM[3+i], i=3,4;
    // - temp is mapped on RAM locations 5-12 (R5, ..., R12): temp[i]=RAM[5+i], i=5,...,12.
    // 
    // static: variable number j in a VM file f as the assembly language symbol f.j.
    // 
    // constant:
    // - truly virtual i.e. not mapped on Hack RAM;
    // - VM access to 'constant i' by supplying the constant i.
    //
    #endregion
    public enum VM_Segment
    {
        Local, Argument, This, That, Pointer, Temp, Static, Constant,
        // The following are not VM segments, but they're stored here for ease of reference.
        R13, R14, R15
    };

    // A delegate for the methods used to determine the command type.
    public delegate Command_Type CommandTypeDelegate(string commandType);    

    public static class Command
    {        
        const string regexSymbol = @"[a-zA-Z_\.\$:][0-9a-zA-Z_\.\$:]*";
        const string regexPosInt = @"[0-9]+";
        // Used in the regex which checks for memory access commands.
        static readonly string regexVirtualMemorySegments;
        
        public static Dictionary<string, Arithmetic_Command> arithmeticCommands = new Dictionary<string, Arithmetic_Command> 
        { 
            { "add", Arithmetic_Command.Add }, { "sub", Arithmetic_Command.Sub }, { "neg", Arithmetic_Command.Neg },
            { "eq", Arithmetic_Command.Eq }, { "gt", Arithmetic_Command.Gt }, { "lt", Arithmetic_Command.Lt }, 
            { "and", Arithmetic_Command.And }, { "or", Arithmetic_Command.Or }, { "not", Arithmetic_Command.Not }
        };        

        public static Dictionary<string, VM_Segment> VMSegments = new Dictionary<string, VM_Segment>
        {
            {"local", VM_Segment.Local}, {"argument", VM_Segment.Argument}, {"this", VM_Segment.This},
            {"that", VM_Segment.That}, {"pointer", VM_Segment.Pointer}, {"temp", VM_Segment.Temp},
            {"static", VM_Segment.Static}, {"constant", VM_Segment.Constant}
        };

        // This list is used each time ArithmeticCommand() is called.
        // Set here once instead of being set every time ArithmeticCommand() is called.
        static List<string> arithmeticCommandsStrings = arithmeticCommands.Keys.ToList<string>(); 

        // The commands are partitioned in the sets below for ease of reference in the methods 
        // which are used to determine the command type.
        static IReadOnlyList<string> memoryAccessCommands = new List<string>() { "push", "pop" };
        static IReadOnlyList<string> programFlowCommands = new List<string>() { "label", "goto", "if-goto" };
        static IReadOnlyList<string> functionCallingCommands = new List<string>() { "function", "call", "return" };        

        // Array of delegates for the methods which are used to determine the command type.
        public static CommandTypeDelegate[] CommandTypeDelegates = new CommandTypeDelegate[] 
            { new CommandTypeDelegate(ArithmeticCommand), new CommandTypeDelegate(MemoryAccessCommand),
              new CommandTypeDelegate(ProgramFlowCommand), new CommandTypeDelegate(FunctionCallingCommand) 
            };
        public static int comTypeDelLength = CommandTypeDelegates.Length;

        static Command()
        {
            regexVirtualMemorySegments = string.Join<string>("|", VMSegments.Keys.ToList<string>());            
        }

        /// <summary>
        /// Checks if the string parameter is a valid arithmetic command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> C_ARITHMETIC, if the input is a valid arithmetic command; ERROR, otherwise.</returns>
        public static Command_Type ArithmeticCommand(string commandText)
        {
            if (arithmeticCommandsStrings.Contains(commandText))
            {
                return Command_Type.C_ARITHMETIC;
            }
            return Command_Type.ERROR;
        }

        /// <summary>
        /// Checks if the string parameter is a valid memory access command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> The command type, if the input is a valid memory access command; ERROR, otherwise.</returns>
        public static Command_Type MemoryAccessCommand(string commandText)
        {
            Command_Type commandType = Command_Type.ERROR;
            string regexPattern = "^(" + String.Join("|", memoryAccessCommands) + ")\\s(" +
                regexVirtualMemorySegments + ")\\s" + regexPosInt + "$";

            string testCommand = RegexMatch(commandText, regexPattern, 1);
            if (testCommand != string.Empty)
            {
                if (testCommand.Equals(memoryAccessCommands[0]))
                {
                    commandType = Command_Type.C_PUSH;
                }
                else
                {
                    commandType = Command_Type.C_POP;
                }
            }
            return commandType;
        }

        /// <summary>
        /// Checks if the string parameter is a valid program flow command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> The command type, if the input is a valid program flow command; ERROR, otherwise.</returns>
        public static Command_Type ProgramFlowCommand(string commandText)
        {
            Command_Type commandType = Command_Type.ERROR;
            string regexPattern = "^(" + String.Join("|", programFlowCommands) + ")\\s" + regexSymbol + "$";

            string testCommand = RegexMatch(commandText, regexPattern, 1);
            if (testCommand != string.Empty)
            {
                if (testCommand.Equals(programFlowCommands[0]))
                {
                    commandType = Command_Type.C_LABEL;
                }
                else if (testCommand.Equals(programFlowCommands[1]))
                {
                    commandType = Command_Type.C_GOTO;
                }
                else
                {
                    commandType = Command_Type.C_IF;
                }
            }
            return commandType;
        }

        /// <summary>
        /// Checks if the string parameter is a valid function calling command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> The command type, if the input is a valid function calling command; ERROR, otherwise.</returns>
        public static Command_Type FunctionCallingCommand(string commandText)
        {
            Command_Type commandType = Command_Type.ERROR;            
            StringBuilder regexPattern = new StringBuilder("^(");
            regexPattern.Append(functionCallingCommands[0]);
            regexPattern.Append("|");
            regexPattern.Append(functionCallingCommands[1]);
            regexPattern.Append(")\\s");
            regexPattern.Append(regexSymbol);
            regexPattern.Append("\\s");
            regexPattern.Append(regexPosInt);
            regexPattern.Append("$");

            // This regex checks for command types C_FUNCTION and C_CALL.
            // If there isn't a match, the command type should be C_RETURN.
            string testCommand = RegexMatch(commandText, regexPattern.ToString(), 1);
            if (testCommand != string.Empty)
            {
                if (testCommand.Equals(functionCallingCommands[0]))
                {
                    commandType = Command_Type.C_FUNCTION;
                }
                else
                {
                    commandType = Command_Type.C_CALL;
                }                
            }
            else
            {
                if (commandText.Equals(functionCallingCommands[2]))
                {
                    commandType = Command_Type.C_RETURN;
                }
            }

            return commandType;
        }

        /// <summary>
        /// Applies a regular expression to a string and returns the specified matched group.
        /// </summary>
        /// <param name="testString"> The string to be processed.</param>
        /// <param name="regExp"> The regex pattern.</param>
        /// <param name="matchGroupNumber"> The index of the matched group.</param>
        /// <returns> A string representing the specified matched group.</returns>
        private static string RegexMatch(string testString, string regExp, int matchGroupNumber)
        {
            string result = string.Empty;
            Match match = Regex.Match(testString, regExp);
            if (match.Success)
            {
                result = match.Groups[matchGroupNumber].Value;
            }

            return result;
        }
    }
}
