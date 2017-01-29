using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VM;

namespace Compiler
{
    public enum Token_Type { KEYWORD, SYMBOL, IDENTIFIER, INT_CONST, STRING_CONST, ERROR };    
    public enum IdentifierCategory { VAR, ARG, STATIC, FIELD, CLASS, SUBROUTINE, NONE };
    public enum Keyword
    {
        CLASS, METHOD, FUNCTION,
        CONSTRUCTOR, INT,
        BOOLEAN, CHAR, VOID,
        VAR, STATIC, FIELD,
        LET, DO, IF, ELSE,
        WHILE, RETURN, TRUE,
        FALSE, NULL, THIS
    };
    public enum Symbol
    {
        OpeningCurlyBracket, ClosingCurlyBracket, OpeningRoundBracket, ClosingRoundBracket,
        OpeningSquareBracket, ClosingSquareBracket, Period, Comma, Semicolon, PlusSign, MinusSign,
        Asterisk, Slash, Ampersand, VerticalBar, LessThanSign, GreaterThanSign, EqualSign, Tilde
    };    

    public static class Token
    {
        public const int MaxValueForIntConst = 32767;
        // Stores the input stream received by the JackTokenizer.
        public static string stringOfTokens = "";
        // (?:) specifies that the group will not be captured.
        // () specifies that the group will be captured.
        // We want to capture only the method name.
        private const string regexMethodDeclarationWoParameterList = @"(?:method )(?:(?:\w)+ )((?:\w)+)";
        private const string regexVoidSubroutineDeclarationWoTypeAndParameterList = @"(?:void )((?:\w)+)";
        // The \G anchor specifies that a match must occur at the position where the previous match ended.
        // When used with the <see cref="Match(string, int)"/> method, the resulting match (if any) starts
        // at the given index in the string and the remainder of the string is not scanned for a match.
        public static readonly List<KeyValuePair<string, Token_Type>> regexStringsByTokenType = new List<KeyValuePair<string, Token_Type>>();
        private const string regexIdentifier = @"\G[a-zA-Z_][0-9a-zA-Z_]* ?";
        private const string regexIntConst = @"\G[0-9]+ ?";
        // The double double-quotes are used to escape the double-quotes in regex.
        // The part " ?" tells the regex to match a space character zero or one time.
        private const string regexStringConst = @"\G""[^\r\n\""]*"" ?";        

        public static Dictionary<string, Keyword> keywordsDictionary = new Dictionary<string, Keyword> 
        { 
            { "class", Keyword.CLASS }, { "method", Keyword.METHOD }, { "function", Keyword.FUNCTION },
            { "constructor", Keyword.CONSTRUCTOR }, { "int", Keyword.INT }, { "boolean", Keyword.BOOLEAN },
            { "char", Keyword.CHAR }, { "void", Keyword.VOID }, { "var", Keyword.VAR },
            { "static", Keyword.STATIC }, { "field", Keyword.FIELD }, { "let", Keyword.LET },
            { "do", Keyword.DO }, { "if", Keyword.IF }, { "else", Keyword.ELSE },
            { "while", Keyword.WHILE }, { "return", Keyword.RETURN }, { "true", Keyword.TRUE },
            { "false", Keyword.FALSE }, { "null", Keyword.NULL }, { "this", Keyword.THIS }
        };

        public static Dictionary<char, Symbol> symbolsDictionary = new Dictionary<char, Symbol>()
        {
            {'{', Symbol.OpeningCurlyBracket}, {'}', Symbol.ClosingCurlyBracket}, {'(', Symbol.OpeningRoundBracket},
            {')', Symbol.ClosingRoundBracket}, {'[', Symbol.OpeningSquareBracket}, {']', Symbol.ClosingSquareBracket},
            {'.', Symbol.Period}, {',', Symbol.Comma}, {';', Symbol.Semicolon}, {'+', Symbol.PlusSign}, 
            {'-', Symbol.MinusSign}, {'*', Symbol.Asterisk}, {'/', Symbol.Slash}, {'&', Symbol.Ampersand}, 
            {'|', Symbol.VerticalBar}, {'<', Symbol.LessThanSign}, {'>', Symbol.GreaterThanSign}, 
            {'=', Symbol.EqualSign}, {'~', Symbol.Tilde}
        };        

        public static Dictionary<string, IdentifierCategory> idenStringToCatDictionary = new Dictionary<string, IdentifierCategory>
        {
            { "var", IdentifierCategory.VAR }, { "static", IdentifierCategory.STATIC }, { "field", IdentifierCategory.FIELD },
        };

        public static Dictionary<Symbol, Arithmetic_Command> binaryAritCommands = new Dictionary<Symbol, Arithmetic_Command>
        {            
            { Symbol.PlusSign, Arithmetic_Command.Add }, { Symbol.MinusSign, Arithmetic_Command.Sub },            
            { Symbol.Ampersand, Arithmetic_Command.And }, { Symbol.VerticalBar, Arithmetic_Command.Or },
            { Symbol.LessThanSign, Arithmetic_Command.Lt }, { Symbol.GreaterThanSign, Arithmetic_Command.Gt },
            { Symbol.EqualSign, Arithmetic_Command.Eq }                    
        };

        public static Dictionary<string, List<string>> voidSubroutineNames = new Dictionary<string, List<string>>
        {
            { "Math", new List<string> { "init" } },
            { "String", new List<string> { "dispose", "setCharAt", "eraseLastChar", "setInt" } },
            { "Array", new List<string> { "dispose"} },
            { "Output", new List<string> { "init", "moveCursor", "printChar", "printString", "printInt", "println", "backspace" } },
            { "Screen", new List<string> { "init", "clearScreen", "eraseLastChar", "setColor", "drawPixel", "drawLine",
                "drawRectangle", "drawCircle" } },
            { "Keyboard", new List<string> { "init" } },
            { "Memory", new List<string> { "init", "poke", "deAlloc" } },
            { "Sys", new List<string> { "init", "halt", "error", "wait" } },
            { "Main", new List<string> { "main" } }
        };

        static Token()
        {
            string regexKeyword = @"\G(" + string.Join<string>("|", keywordsDictionary.Keys.ToList<string>()) + @") ?";
            string regexSymbol = @"\G(" + Regex.Escape(string.Join<char>(" ", symbolsDictionary.Keys));
            // The <see cref="Escape(string)"/> method escaped the white spaces.
            // So we need to replace "\ " instead of " ".
            regexSymbol = regexSymbol.Replace(@"\ ", "|") + @") ?";
            
            regexStringsByTokenType.Add(new KeyValuePair<string, Token_Type>(regexKeyword, Token_Type.KEYWORD));
            regexStringsByTokenType.Add(new KeyValuePair<string, Token_Type>(regexSymbol, Token_Type.SYMBOL));
            regexStringsByTokenType.Add(new KeyValuePair<string, Token_Type>(regexIdentifier, Token_Type.IDENTIFIER));
            regexStringsByTokenType.Add(new KeyValuePair<string, Token_Type>(regexIntConst, Token_Type.INT_CONST));
            regexStringsByTokenType.Add(new KeyValuePair<string, Token_Type>(regexStringConst, Token_Type.STRING_CONST));            
        }

        /// <summary>
        /// Matches a token starting at the specified position in the string of tokens.
        /// </summary>
        /// <param name="index"> The position in the string where the search should begin.</param>
        /// <returns> Matched token string and type; error values otherwise.</returns>
        public static KeyValuePair<string, Token_Type> GetCurrentToken(int index)
        {
            int i=0;
            int tokenTypeCount = regexStringsByTokenType.Count;
            string tokenString = "";
            Token_Type tokenType = Token_Type.ERROR;

            // While a match hasn't been found and there still are token types to try.
            while(string.IsNullOrEmpty(tokenString) && i<tokenTypeCount)
            {
                tokenString = RegexMatch(stringOfTokens, regexStringsByTokenType[i].Key, index);
                i++;
            }

            // If a match has been found.
            if (!string.IsNullOrEmpty(tokenString) && i <= tokenTypeCount)
            {                
                tokenType = regexStringsByTokenType[i-1].Value;
                if (tokenType == Token_Type.INT_CONST)
                {
                    int value;
                    bool tryInt = Int32.TryParse(tokenString, out value);
                    if (!(tryInt && 0 <= value && value <= MaxValueForIntConst))
                    {
                        tokenType = Token_Type.ERROR;
                    }
                }
                else if (tokenType == Token_Type.KEYWORD)
                {
                    // Try to match for an identifier.
                    // If a match is found and the identifier is not a keyword, go with the identifier.
                    // This is necessary to avoid cases like "Main.double" being parsed as "Main", ".", "do".
                    string tokenIdenString = "";
                    KeyValuePair<string, Token_Type> pair = regexStringsByTokenType.First(p => p.Value == Token_Type.IDENTIFIER);
                    tokenIdenString = RegexMatch(stringOfTokens, pair.Key, index);
                    string trimmedTokenIdenString = tokenIdenString.Trim();
                    if (!string.IsNullOrEmpty(trimmedTokenIdenString))
                    {
                        Keyword value;
                        bool isKeyword = keywordsDictionary.TryGetValue(trimmedTokenIdenString, out value);
                        if (!isKeyword)
                        {
                            tokenString = tokenIdenString;
                            tokenType = Token_Type.IDENTIFIER;
                        }
                    }
                }
            }            
            else
            {
                // Get the character at the position starting from which no token has been found.
                // This will be useful when the error message will be displayed to the user.
                tokenString = stringOfTokens[index].ToString();
            }

            return new KeyValuePair<string,Token_Type>(tokenString, tokenType);
        }

        /// <summary>
        /// Gets the names of all the methods in the current Jack file.
        /// </summary>
        /// <returns> A list containing the method names.</returns>
        public static List<string> GetMethodsInClass()
        {
            List<string> methodNames = new List<string>();

            MatchCollection matchList = Regex.Matches(stringOfTokens, regexMethodDeclarationWoParameterList);
            methodNames = matchList.Cast<Match>().Select(match => match.Groups[1].Value).ToList();            

            return methodNames;
        }

        /// <summary>
        /// Adds, to the list of void subroutine names, the items found in all classes of the Jack program.
        /// </summary>
        /// <param name="cleanJackCodeBytesList"> Clean byte array of each Jack file in the program.</param>
        public static void SetVoidSubroutinesList(Dictionary<string, byte[]> cleanJackCodeBytesList)
        {
            StreamReader reader;
            string className;
            MatchCollection matchList;

            foreach (var item in cleanJackCodeBytesList)
            {
                using (MemoryStream jackStream = new MemoryStream(item.Value))
                {
                    className = Path.GetFileNameWithoutExtension(item.Key);
                    // If the subroutines of this class exist in the dictionary, don't process the class.
                    if (!voidSubroutineNames.Keys.Contains(className))
                    {
                        reader = new StreamReader(jackStream);
                        string stringOfTokens = reader.ReadToEnd();
                        matchList = Regex.Matches(stringOfTokens, regexVoidSubroutineDeclarationWoTypeAndParameterList);
                        voidSubroutineNames[className] = matchList.Cast<Match>().Select(match => match.Groups[1].Value).ToList();
                    }
                }
            }
        }

        /// <summary>
        /// Applies a regular expression to a substring starting at the specified position in a string.
        /// </summary>
        /// <param name="testString"> The string to be processed.</param>
        /// <param name="regExp"> The regex pattern.</param>
        /// <param name="index"> The position in the string where the search should begin.</param>
        /// <returns> Returns the first matched group.</returns>
        private static string RegexMatch(string testString, string regExp, int index)
        {
            string result = string.Empty;
            Regex num = new Regex(regExp);
            var match = num.Match(testString, index);
            if (match.Success)
            {
                result = match.Value;
            }                       

            return result;
        }
    }
}
