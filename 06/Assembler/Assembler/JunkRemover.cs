using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assembler
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
        /// Removes comments, space characters and empty lines.
        /// A comment is text beginning with two slashes (//) and ending at the end of the line.
        /// </summary>        
        /// <returns> A byte array containing the clean stream.</returns>
        public byte[] RemoveJunk()
        {            
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(outputStream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        // Remove all comments.
                        Regex regex = new Regex(@"\/\/.*");
                        line = regex.Replace(line, "");
                        // Remove all white spaces.
                        regex = new Regex(@"\s+");
                        line = regex.Replace(line, "");
                        // Ignore empty lines.
                        if (!String.IsNullOrEmpty(line))
                        {                            
                            writer.WriteLine(line);
                        }
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
