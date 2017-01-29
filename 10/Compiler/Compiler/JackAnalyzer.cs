using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Compiler
{
    /// <summary>
    /// Top level driver that sets up and invokes the other modules.
    /// </summary>
    public class JackAnalyzer
    {
        /// <summary>
        /// Gets or sets a list containing the Jack file(s) to be analyzed.
        /// </summary>
        /// <value> The Jack file(s) to be analyzed.</value>
        private List<string> JackFilesPaths
        {
            get;
            set;
        }

        public JackAnalyzer(List<string> jackFilesPaths)
        {
            JackFilesPaths = jackFilesPaths;          
        }

        /// <summary>
        /// Analyzes the input Jack file(s) and outputs the VM code in-memory stream(s).
        /// </summary>
        /// <returns> A list of VM in-memory streams containing the VM translation of the Jack files.</returns>
        public List<MemoryStream> Analyze()
        {
            List<MemoryStream> vmStreams = new List<MemoryStream>();

            #region TokenizerXmlOutputSettings
            //XmlWriterSettings settings = new XmlWriterSettings();
            //// The Unicode encoding ensures that the following Jack tokens: '<', '>', '"' and '&',
            //// which are also used for XML markup, are output as '&lt;', '&gt;', '&quot;', and '&amp;', respectively.            
            //settings.Encoding = Encoding.Unicode;
            //settings.Indent = true;
            ////settings.IndentChars = "";
            //settings.NewLineOnAttributes = true;
            #endregion

            Dictionary<string, byte[]> cleanJackCodeBytesDict = new Dictionary<string, byte[]>();

            foreach (string jackFilePath in JackFilesPaths)
            {
                // The byte array will consist of the succession of tokens from the Jack file,
                // with zero or one space between them.
                byte[] cleanJackCodeBytes = this.RemoveJunk(jackFilePath);
                cleanJackCodeBytesDict[jackFilePath] = cleanJackCodeBytes;
            }

            Token.SetVoidSubroutinesList(cleanJackCodeBytesDict);

            foreach(var item in cleanJackCodeBytesDict)
            {
                #region TokenizerXmlOutputWrite
                /* 
                 * ******************************************************************************************
                 * 
                 * This code section writes xml files resulting from the Tokenizer.
                 * 
                 * ******************************************************************************************
                 */

                //MemoryStream tokensInJackFileXml = new MemoryStream();
                //using (XmlWriter writer = XmlWriter.Create(tokensInJackFileXml, settings))
                //{                   
                //    writer.WriteStartElement("tokens");

                //    // The byte array will consist of the succession of tokens from the Jack file,
                //    // with zero or one space between them.
                //    byte[] cleanJackCodeBytes = this.RemoveJunk(jackFilePath);
                //    using (MemoryStream jackStream = new MemoryStream(cleanJackCodeBytes))
                //    {
                //        using (JackTokenizer tokenizer = new JackTokenizer(jackStream))
                //        {
                //            while (tokenizer.HasMoreTokens())
                //            {
                //                tokenizer.Advance();
                //                Token_Type tokenType = tokenizer.TokenType();

                //                switch (tokenType)
                //                {
                //                    case Token_Type.KEYWORD:
                //                        {
                //                            writer.WriteStartElement("keyword");
                //                            writer.WriteValue(" " + Token.keywordsDictionary.FirstOrDefault
                //                                (x => x.Value == tokenizer.Keyword()).Key + " ");
                //                            writer.WriteEndElement();
                //                            break;
                //                        }
                //                    case Token_Type.SYMBOL:
                //                        {
                //                            writer.WriteStartElement("symbol");
                //                            writer.WriteValue(" " + tokenizer.Symbol().ToString() + " ");
                //                            writer.WriteEndElement();
                //                            break;
                //                        }
                //                    case Token_Type.IDENTIFIER:
                //                        {
                //                            writer.WriteStartElement("identifier");
                //                            writer.WriteValue(" " + tokenizer.Identifier() + " ");
                //                            writer.WriteEndElement();
                //                            break;
                //                        }
                //                    case Token_Type.INT_CONST:
                //                        {
                //                            writer.WriteStartElement("integerConstant");
                //                            writer.WriteValue(" " + tokenizer.IntVal() + " ");
                //                            writer.WriteEndElement();
                //                            break;
                //                        }
                //                    case Token_Type.STRING_CONST:
                //                        {
                //                            writer.WriteStartElement("stringConstant");
                //                            writer.WriteValue(" " + tokenizer.StringVal() + " ");
                //                            writer.WriteEndElement();
                //                            break;
                //                        }
                //                    default:
                //                        {
                //                            //Token type is ERROR.                                            
                //                            throw new Exception("Token starting with character '" + tokenizer.currentToken + 
                //                            "' is invalid.");
                //                        }
                //                }
                //            }
                //        }
                //    }

                //    writer.WriteEndElement();
                //    writer.WriteEndDocument();
                //}
                #endregion

                try
                {
                    using (MemoryStream jackStream = new MemoryStream(item.Value))
                    {
                        MemoryStream vmStream = new MemoryStream();
                        using (CompilationEngine parser = new CompilationEngine(jackStream, ref vmStream))
                        {
                            vmStreams.Add(vmStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error in " + Path.GetFileName(item.Key) + ": " + ex.Message);
                }
            }

            return vmStreams;
        }

        /// <summary>
        /// Removes junk from the stream i.e. removes block and line comments, and
        /// replaces whitespace characters with a single space character.
        /// </summary>
        /// <returns> Returns a byte array.</returns>
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
