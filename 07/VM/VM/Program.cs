using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM
{    
    class Program
    {
        public const string VmFileExtension = ".vm";
        public const string AsmFileExtension = ".asm";

        static void Main(string[] args)
        {
            Console.WriteLine("This program generates assembly code from virtual machine code."
            + Environment.NewLine + "Specify the path for a .vm file or a directory containing .vm files:");
            string inputPath = Console.ReadLine();

            try
            {
                string asmFilePath = "";
                bool isFile = File.Exists(inputPath);
                bool isDirectory = Directory.Exists(inputPath);
                if (isFile || isDirectory)
                {
                    List<string> vmFiles = new List<string>();
                    if (isFile)
                    {
                        vmFiles.Add(inputPath);
                        asmFilePath = Path.GetFullPath(Path.ChangeExtension(inputPath, AsmFileExtension));
                    }
                    else
                    {
                        // Process the list of files found in the directory.
                        string[] fileEntries = Directory.GetFiles(inputPath);
                        string extension;
                        // Get the .vm files from the specified directory.
                        foreach (string filePath in fileEntries)
                        {
                            extension = Path.GetExtension(filePath);
                            if (extension.Equals(VmFileExtension))
                            {
                                vmFiles.Add(filePath);
                            }
                        }

                        string dirName = new DirectoryInfo(inputPath).Name;
                        asmFilePath = Path.Combine(inputPath, Path.ChangeExtension(dirName, AsmFileExtension));
                    }
                    
                    VMTranslator translator = new VMTranslator(vmFiles);
                    List<string> assemblyCodeLines = translator.Translate();
                    WriteToFile(asmFilePath, assemblyCodeLines);
                    
                    Console.WriteLine(Environment.NewLine + "VM translation complete." + Environment.NewLine + 
                        "The assembly file was saved in the directory containing the .vm file(s).");
                }
                else
                {
                    Console.WriteLine("The specified path doesn't correspond to an existing file or directory.");
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
        private static void WriteToFile(string filePath, IList<string> contents)
        {
            File.Delete(filePath);
            File.AppendAllLines(filePath, contents);
        }        
    }
}
