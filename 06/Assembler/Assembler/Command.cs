using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assembler
{
    // The types of possible commands in the Hack assembly language
    // and an error command.
    public enum Command_Type { A_COMMAND, C_COMMAND, L_COMMAND, ERROR };
    public struct C_COMMANDFields
    {
        public string dest;
        public string comp;
        public string jump;

        public C_COMMANDFields(string defaultValue)
        {
            dest = comp = jump = defaultValue;
        }
    }

    public static class Command
    {
        public const int AddressNumberOfBits = 15;
        // 32767=2^15-1 i.e. the maximum number of addresses on 15 bits.
        public const int MaxValueInARegister = 32767;
        private static string regexSymbol = @"[a-zA-Z_\.\$:][0-9a-zA-Z_\.\$:]*";
        private static string regexAddress = @"[0-9]+";

        public static Dictionary<string, string> destMnemonics = new Dictionary<string, string>
          { { "", "000" }, { "M", "001" }, { "D", "010" }, { "MD", "011" },
            { "A", "100" }, { "AM", "101" }, { "AD", "110" }, { "AMD", "111" } };
        public static Dictionary<string, string> compMnemonics = new Dictionary<string, string> 
         {  { "0", "0101010" }, { "1", "0111111" }, { "-1", "0111010" }, { "D", "0001100" },
            { "A", "0110000" }, { "!D", "0001101" }, { "!A", "0110001" }, { "-D", "0001111" },
            { "-A", "0110011" }, { "D+1", "0011111" }, { "A+1", "0110111" }, { "D-1", "0001110" },
            { "A-1", "0110010" }, { "D+A", "0000010" }, { "D-A", "0010011" }, { "A-D", "0000111" },
            { "D&A", "0000000" }, { "D|A", "0010101" }, 
            { "M", "1110000" }, { "!M", "1110001" }, { "-M", "1110011" }, { "M+1", "1110111" },
            { "M-1", "1110010" }, { "D+M", "1000010" }, { "D-M", "1010011" }, { "M-D", "1000111" },
            { "D&M", "1000000" }, { "D|M", "1010101" } };
        public static Dictionary<string, string> jumpMnemonics = new Dictionary<string, string>
        { { "", "000" }, { "JGT", "001" }, { "JEQ", "010" }, { "JGE", "011" },
          { "JLT", "100" }, { "JNE", "101" }, { "JLE", "110" }, { "JMP", "111" } };

        /// <summary>
        /// Checks if the structure of the string parameter complies with the A-instruction format.
        /// A-instruction: @value, 
        /// where value: non-negative integer represented on 15 bits or symbol.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> True, if the input is a valid A-COMMAND; false, otherwise.</returns>
        public static bool IsA_COMMAND(string commandText)
        {
            // If A-instruction is @value, check that the value complies to address boundaries.
            string testAddress = RegexMatch(commandText, "^@" + regexAddress + "$", 0);
            if (testAddress != string.Empty)
            {
                int value;
                bool result = Int32.TryParse(testAddress.Substring(1), out value);
                if (result && 0 <= value && value <= MaxValueInARegister)
                {
                    return true;
                }
            }

            string testSymbol = RegexMatch(commandText, "^@" + regexSymbol + "$", 0);
            if (testSymbol != string.Empty)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the structure of the string parameter complies with the C-instruction format.
        /// C-instruction: dest=comp;jump,
        /// where dest: destination where the value computed by the comp part is to be stored,
        ///       comp: function from the fixed set of functions supported by the Hack ALU,
        ///       jump: condition which specifies the next instruction to be executed.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> True, if the input is a valid C-COMMAND; false, otherwise.</returns>
        public static bool IsC_COMMAND(string commandText)
        {
            C_COMMANDFields fields = GetC_COMMANDFields(commandText);
            if (destMnemonics.ContainsKey(fields.dest) && compMnemonics.ContainsKey(fields.comp) &&
                    jumpMnemonics.ContainsKey(fields.jump))
            {
                return true;
            }            
            return false;
        }

        /// <summary>
        /// Checks if the structure of the string parameter complies with the label declaration
        /// format, given as the pseudo-command
        /// (Symbol), where Symbol: sequence of letters, digits, '_', '.', '$' and ':' 
        /// that does not begin with a digit.    
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> True, if the input is a valid L-COMMAND; false, otherwise.</returns>
        public static bool IsL_COMMAND(string commandText)
        {            
            string regexL_COMMAND = @"^\(" + regexSymbol + @"\)$";            
            string test = RegexMatch(commandText, regexL_COMMAND, 0);            
            if (test!=string.Empty)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Extracts the 3 fields of the command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> Struct object containing the extracted fields.</returns>
        public static C_COMMANDFields GetC_COMMANDFields(string commandText)
        {
            C_COMMANDFields fields = new C_COMMANDFields(string.Empty);
            if (commandText.Contains("="))
            {
                // dest is not empty and the command has the form dest=comp.
                var splitArray = commandText.Split('=');
                fields.dest = splitArray[0];
                fields.comp = splitArray[1];
            }
            else
            {
                // dest is empty. Need to check that jump is not also empty.
                if (commandText.Contains(";"))
                {
                    var splitArray = commandText.Split(';');
                    fields.comp = splitArray[0];
                    fields.jump = splitArray[1];
                }
            }
            return fields;
        }

        /// <summary>
        /// Extracts the symbol from the command.
        /// </summary>
        /// <param name="commandText"> String of command.</param>
        /// <returns> String containing the extracted symbol.</returns>
        public static string GetSymbol(string commandText)
        {
            string symbol = string.Empty;
            string test = RegexMatch(commandText, regexSymbol, 0);
            if (test != string.Empty)
            {
                symbol = test;
            }
            else
            {
                test = RegexMatch(commandText, regexAddress, 0);
                if (test != string.Empty)
                {
                    symbol = test;
                }
            }

            return symbol;
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
