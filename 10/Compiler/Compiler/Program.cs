using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class Program
    {
        public const string JackFileExtension = ".jack";
        public const string VmFileExtension = ".vm";

        static void Main(string[] args)
        {
            Console.WriteLine("This program generates a corresponding VM file for each Jack file."
            + Environment.NewLine + "Specify the path for a Jack file or a directory containing Jack files:");
            string inputPath = Console.ReadLine();
            
            try
            {
                bool isFile = File.Exists(inputPath);
                bool isDirectory = Directory.Exists(inputPath);
                if (isFile || isDirectory)
                {
                    List<string> jackFilesPaths = new List<string>();
                    List<string> vmFilesPaths = new List<string>();
                    if (isFile)
                    {
                        jackFilesPaths.Add(inputPath);
                        string fileName = Path.GetFileNameWithoutExtension(inputPath);
                        string filePath = Path.GetDirectoryName(inputPath);
                        vmFilesPaths.Add(Path.Combine(filePath, Path.ChangeExtension(fileName, VmFileExtension)));
                    }
                    else
                    {
                        // Process the list of files found in the directory.
                        string[] fileEntries = Directory.GetFiles(inputPath);                        
                        string extension;                        
                        foreach (string filePath in fileEntries)
                        {
                            extension = Path.GetExtension(filePath);
                            if (extension.Equals(JackFileExtension))
                            {
                                jackFilesPaths.Add(filePath);
                                string fileName = Path.GetFileNameWithoutExtension(filePath);
                                vmFilesPaths.Add(Path.Combine(inputPath, Path.ChangeExtension(fileName, VmFileExtension)));
                            }
                        }                                                
                    }

                    // Use a JackAnalyzer instance to compile the Jack file(s).
                    JackAnalyzer analyzer = new JackAnalyzer(jackFilesPaths);
                    List<MemoryStream> compiledJackFilesStreams = analyzer.Analyze();
                    for (int i = 0; i < compiledJackFilesStreams.Count; i++)
                    {
                        WriteToFile(vmFilesPaths[i], compiledJackFilesStreams[i]);
                    }

                    Console.WriteLine(Environment.NewLine + "Compilation complete." + Environment.NewLine
                        + "The VM file(s) were saved in the directory containing the Jack file(s).");
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
        private static void WriteToFile(string filePath, MemoryStream fileContent)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileContent.WriteTo(file);
            }
        }
    }
}
