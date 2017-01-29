using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    /// <summary>
    /// Breaks the input stream into Jack language tokens, as specified by the Jack grammar.
    /// Starting at position i, it tries to match regexes for terminals, where i is initially 0.
    /// If a match is found, the position advances by the length of the match.
    /// Else, the input stream is invalid according to the Jack grammar specification for terminals.
    /// </summary>
    public class JackTokenizer : IDisposable
    {
        private StreamReader reader;
        private int currentTokenIndex = 0;
        private int tokensInputLength;
        private KeyValuePair<string, Token_Type> currentTokenInfo;

        public string CurrentToken
        {
            get;
            private set;
        }

        /// <summary>
        /// Opens the input file/stream and gets ready to tokenize it.
        /// </summary>
        /// <param name="jackFileStream"> Stream of the Jack file to be tokenized.</param>
        public JackTokenizer(Stream jackFileStream)
        {
            reader = new StreamReader(jackFileStream);
            string stringOfTokens = reader.ReadToEnd();
            tokensInputLength = stringOfTokens.Length;
            // Set this Token variable to avoid passing the string with the Jack file contents each time a match is tried.
            // Only the current position in the string of tokens needs to be passed.
            Token.stringOfTokens = stringOfTokens;
        }

        /// <summary>
        /// Checks if there are more tokens in the file.
        /// </summary>
        /// <returns> True, if there are more tokens; false, otherwise.</returns>
        public bool HasMoreTokens()
        {
            return !(currentTokenIndex==tokensInputLength);
        }

        /// <summary>
        /// Gets the next token from the input and makes it the current token.
        /// This method should only be called if hasMoreTokens() is true.
        /// Initially there is no current token. 
        /// </summary>
        public void Advance()
        {
            currentTokenInfo = Token.GetCurrentToken(currentTokenIndex);
            CurrentToken = currentTokenInfo.Key;
            currentTokenIndex += CurrentToken.Length;
            // A token is matched along with a potential space character at the end.
            CurrentToken = CurrentToken.TrimEnd();
        }

        /// <summary>
        /// Looks ahead at the next token without advancing.
        /// This method should only be called if hasMoreTokens() is true.
        /// </summary>
        /// <returns> The next token and its type.</returns>
        public KeyValuePair<string, Token_Type> Peek()
        {
            currentTokenInfo = Token.GetCurrentToken(currentTokenIndex);
            string key = currentTokenInfo.Key;

            return new KeyValuePair<string, Token_Type>(key.TrimEnd(), currentTokenInfo.Value);
        }

        /// <summary>
        /// Returns the character which is the next token.
        /// Should be called only when tokenType() is SYMBOL. 
        /// </summary>
        /// <returns></returns>
        public Symbol PeekSymbol(string tokenString)
        {
            return Token.symbolsDictionary[Convert.ToChar(tokenString)];
        }

        /// <summary>
        /// Returns the keyword which is the next token.
        /// Should be called only when tokenType() is KEYWORD. 
        /// </summary>
        /// <returns></returns>
        public Keyword PeekKeyword(string tokenString)
        {
            return Token.keywordsDictionary[tokenString];
        }

        /// <summary>
        /// Returns the type of the current token:
        /// KEYWORD;
        /// SYMBOL;
        /// IDENTIFIER: sequence of letters, digits, and underscore ( '_' ) not starting with a digit;
        /// INT_CONST: decimal number in the range 0 .. 32767=2^15-1;
        /// STRING_CONST: '"' sequence of Unicode characters not including double quotes or newline '"'.
        /// </summary>
        /// <returns> The token type.</returns>
        public Token_Type TokenType()
        {
            return currentTokenInfo.Value;            
        }

        /// <summary>
        /// Returns the keyword which is the current token.
        /// Should be called only when tokenType() is KEYWORD. 
        /// </summary>
        /// <returns></returns>
        public Keyword Keyword()
        {            
            return Token.keywordsDictionary[CurrentToken];
        }

        /// <summary>
        /// Returns the character which is the current token.
        /// Should be called only when tokenType() is SYMBOL. 
        /// </summary>
        /// <returns></returns>
        public Symbol Symbol()
        {           
            return Token.symbolsDictionary[Convert.ToChar(CurrentToken)];
        }

        /// <summary>
        /// Returns the identifier which is the current token.
        /// Should be called only when tokenType() is IDENTIFIER. 
        /// </summary>
        /// <returns></returns>
        public string Identifier()
        {            
            return CurrentToken;
        }

        /// <summary>
        /// Returns the integer value of the current token.
        /// Should be called only when tokenType() is INT_CONST. 
        /// </summary>
        /// <returns></returns>
        public int IntVal()
        {            
            return Convert.ToInt32(CurrentToken);
        }

        /// <summary>
        /// Returns the string value of the current token, without the double quotes.
        /// Should be called only when tokenType() is STRING_CONST. 
        /// </summary>
        /// <returns></returns>
        public string StringVal()
        {
            return CurrentToken.Trim('"');
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
