using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoveCRLFFromItems
{
    class Program
    {
        private const string NEW_LINE_DELIMITER = "\r\n";
        private static readonly char[] _newLineDelimiters = { '\r', '\n' };
        private static readonly char[] _itemsToClean = { '\r', '\n' };

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("\r\n   [sourceFile]\t[outputDir]\tCreates 'CLEAN' version of\t\t\t\t\t\t\tspecified file");
                    Console.WriteLine("\r\n-p [sourceDir]\t[outputDir]\tIn parallel processes all files in\t\t\t\t\t\tdirectory and outputs 'CLEAN' versions");
                    return;
                }

                var inputPath = args[1]; ;

                var tmpop = args[2].Replace("\"", string.Empty);
                var outputPath = tmpop.EndsWith("\\") ? tmpop : tmpop + "\\"; ;

                var inParallel = args[0].ToLower() == "-p";
                var canProcess = File.Exists(inputPath) && Directory.Exists(outputPath);

                if (canProcess)
                {
                    if (inParallel)
                    {
                        var files = Directory.GetFiles(inputPath, "*", SearchOption.TopDirectoryOnly);
                        Parallel.ForEach(files, f =>
                        {
                            ProcessFileAndOutput(f, outputPath);
                        });
                    }
                    else
                    {
                        ProcessFileAndOutput(inputPath, outputPath);
                    } 
                }
                else
                {
                    Console.WriteLine("\r\nInvalid paths. \r\nInput File: \"{0}\"\r\nOutput Dir.: \"{1}\"", inputPath, outputPath);
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\n" + e.Message);
            }
        }

        private static void ProcessFileAndOutput(string inputFile, string outputDirectory)
        {
            var status = ProcessFile(inputFile, outputDirectory);

            Console.WriteLine(status == -1
                ? "Unexpected character during file processing"
                : "File prcessing complete");
        }

        private static int ProcessFile(string path, string outputPath)
        {
            var ext = System.IO.Path.GetExtension(path);
            string outputFileName = System.IO.Path.GetFileNameWithoutExtension(path) + "_CLEAN_.txt";

            using (var sw = new StreamWriter(outputPath + outputFileName, true))
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    return ProcessStream(fs, sw);
                }
            }
        }
        
        /// <summary>
        /// Reads input stream and removes unwanted items. 
        /// </summary>
        /// <param name="fs">Stream reader</param>
        /// <param name="sw">Stream writer</param>
        /// <returns>Final state. 1 is success, -1 is failure</returns>
        public static int ProcessStream(FileStream fs, StreamWriter sw)
        {
            // set default values for 
            // processing variables
            var index = 1;
            var isEof = false;
            var finalStatus = 0;
            var c = '\0';

            // set stream to beginning
            fs.Seek(0, SeekOrigin.Begin);

            // set initial state
            var q = 0;

            while (isEof == false)
            {// begin reading the stream
                c = (char)fs.ReadByte();

                if ((q == 0 || q == 4) && c == '"')
                {// opening quotes of item
                    sw.Write(c);
                    q = 1;
                }
                else if ((q == 1 || q == 3) && c == '"')
                {// closing quote of item
                    sw.Write(c);
                    q = 2;
                }
                else if ((q == 1 || q == 3 || q == 4) && (toClean = Program._itemsToClean.Contains(c)))
                {// skip ignore values; replace new lines with space...prevents string concatination
                    if (toClean)
                        sw.Write(" ");

                    q = 3;
                }
                else if (q == 2 && c == ',')
                {// transition back to initial state
                    sw.Write(c);
                    q = 0;
                }
                else if (q == 2 && Program._newLineDelimiters.Contains(c))
                {// reached end of the line

                    sw.Write(Program.NEW_LINE_DELIMITER);

                    q = 4;
                    Console.WriteLine(++index);
                }
                else if (q == 1 || q == 2 || q == 3)
                {// valid character write to file
                    sw.Write(c);
                    q = 1;
                }
                else if ((error = q == 0) || c == char.MaxValue)
                {// exit reader...
                    if (error)
                        finalStatus = -1;
                    else
                        finalStatus = 1;

                    isEof = true;
                }
            }// end while

            return finalStatus;
        }// end function     
    }
}
