using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    /// <summary>
    /// Removes comments, space characters and empty lines.
    /// </summary>
    public class JunkRemover : IDisposable
    {
        private StreamReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="JunkRemover"/> class.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        public JunkRemover(Stream inputStream)
        {
            reader = new StreamReader(inputStream);
        }

        /// <summary>
        /// Removes comments and replaces whitespace characters, in excess or not, with a single space character.
        /// Comments are of the standard formats:
        ///     /* comment until closing */, /** API comment */, and // comment to end of line.
        /// Whitespace characters are: space, horizontal tab, form-feed, newline, return carriage, vertical tab.
        /// </summary>        
        /// <returns> A byte array containing the clean stream.</returns>
        public byte[] RemoveJunk()
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(outputStream))
                {
                    string fileContentsString = reader.ReadToEnd();

                    // Remove all block comments.
                    // The non-greedy matcher '*?' ensures the regex doesn't match too much:
                    // e.g. 2 subsequent comments matched as one single comment, thus covering everything in between.
                    Regex regex = new Regex(@"/\*(.|[\r\n])*?\*/"); // this one also works: @"\/\*[\w\W]*?\*\/"
                    fileContentsString = regex.Replace(fileContentsString, "");
                    // Remove all line comments.
                    // The '.' metacharacter matches any single character except a newline character.
                    // Thus, no need to append a newline character to the end of the regex.
                    regex = new Regex(@"//.*");
                    fileContentsString = regex.Replace(fileContentsString, "");
                    // This regex catches any kind of whitespace (e.g. tabs, newlines, etc.).
                    regex = new Regex(@"\s+");
                    fileContentsString = regex.Replace(fileContentsString, " ");
                    // This is necessary because every kind of whitespace has been replaced with a single space;
                    // that means the line potentially has a leading and/or trailing white space.
                    fileContentsString = fileContentsString.Trim();
                    // Ignore empty files.
                    if (!String.IsNullOrEmpty(fileContentsString))
                    {
                        writer.Write(fileContentsString);
                    }

                    writer.Flush();
                }

                return outputStream.ToArray();
            }
        }

        void IDisposable.Dispose()
        {
            reader.Dispose();
        }
    }
}
