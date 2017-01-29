using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("This program generates a binary code file from an assembly file."
            + Environment.NewLine + "Specify the path for an .asm file:");
            string asmFilePath = Console.ReadLine();

            try
            {
                if (File.Exists(asmFilePath))
                {
                    Assembler assembler = new Assembler(asmFilePath);
                    string assembledCode = assembler.Assemble();

                    string hackFilePath = Path.GetFullPath(Path.ChangeExtension(asmFilePath, "hack"));
                    Program.WriteToFile(hackFilePath, assembledCode);
                    Console.WriteLine("Assembly complete.");
                }
                else
                {
                    Console.WriteLine("The specified file doesn't exist.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Writes to file.
        /// </summary>
        /// <param name="filePath"> The file path.</param>
        /// <param name="fileContent"> The content to be written.</param>
        private static void WriteToFile(string filePath, string fileContent)
        {
            File.Delete(filePath);
            File.AppendAllText(filePath, fileContent);
        }
    }
}
