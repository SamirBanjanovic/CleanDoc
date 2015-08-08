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
        private static readonly char[] _newLineDelimiters = { '\r', '\n' };
        private static readonly char[] _itemsToClean = { '\r', '\n' };

        static void Main(string[] args)
        {
            try
            {
#if DEBUG                
                
                string path = @"c:\users\samir\desktop\test.txt";                
                string outputPath = @"c:\users\samir\desktop\";
                bool inParallel = false;                
                
#else

                if (args.Length == 0)
                {
                    Console.WriteLine("\r\n   [sourceFile]\t[outputDir]\tCreates 'CLEAN' version of\t\t\t\t\t\t\tspecified file");
                    Console.WriteLine("\r\n-p [sourceDir]\t[outputDir]\tIn parallel processes all files in\t\t\t\t\t\tdirectory and outputs 'CLEAN' versions");
                    return;
                }

                string path;
                string outputPath;
                bool inParallel = args[0].ToUpper() == "-p";

#endif

                if (inParallel)
                {
                    path = args[1];

                    var tmpop = args[2].Replace("\"", string.Empty);
                    outputPath = tmpop.EndsWith("\\") ? tmpop : tmpop + "\\";

                    if (CanProcessFile(path, outputPath))
                    {
                        // get list of files
                        var files = System.IO.Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                        Parallel.ForEach(files, f =>
                        {
                            ProcessFile(f, outputPath);
                        });
                    }
                    else
                    {
                        Console.WriteLine("\r\nInvalid paths. \r\nInput File: \"{0}\"\r\nOutput Dir.: \"{1}\"", path, outputPath);
                    }
                }
                else
                {

#if !DEBUG

                    path = args[0].Replace("\"", string.Empty);
                    var tmpop = args[1].Replace("\"", string.Empty);
                    outputPath = tmpop.EndsWith("\\") ? tmpop : tmpop + "\\";

#endif

                    if (CanProcessFile(path, outputPath))
                    {
                        ProcessFile(path, outputPath);
                    }
                    else
                    {
                        Console.WriteLine("\r\nInvalid paths. \r\nInput File: \"{0}\"\r\nOutput Dir.: \"{1}\"", path, outputPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n\r\n" + e.Message);
            }
        }

        private static void ProcessFile(string path, string outputPath)
        {
            var ext = System.IO.Path.GetExtension(path);
            string outputFileName = System.IO.Path.GetFileNameWithoutExtension(path) + "_CLEAN_.txt";

            using (StreamWriter sw = new StreamWriter(outputPath + outputFileName, true))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    CleanCopyUsingStateMachine(fs, sw);
                }
            }
        }

        private static bool CanProcessFile(string inputFile, string outputPath)
        {
            return System.IO.File.Exists(inputFile) && System.IO.Directory.Exists(outputPath);

            /*
                if (!System.IO.File.Exists(inputFile))
                {
                    Console.WriteLine("\"{0}\" File does not exist!!!", inputFile);
                    return;
                }
                else if (!System.IO.Directory.Exists(outputPath))
                {
                    Console.WriteLine("\"{0}\" Directory does not exist!!!", outputPath);
                    return;
                }
            */
        }

        public static void CleanCopyUsingStateMachine(FileStream fs, StreamWriter sw)
        {
            int index = 1;
            bool isEOF = false;
            char c = '\0';

            // set stream to beginning
            fs.Seek(0, SeekOrigin.Begin);

            // set initial state
            int q = 0;
            
            while (isEOF == false)
            {// begin reading the stream
                c = (char)fs.ReadByte();
                
                if ((q == 0 || q == 4) && c == '"')
                {// opening quotes of item
                    sw.Write(c);
                    q = 1;
                }
                else if ((q == 1 || q == 3 ) && c == '"')
                {// closing quote of item
                    sw.Write(c);
                    q = 2;
                }
                else if((q == 1 || q == 3 || q == 4) && Program._itemsToClean.Contains(c))
                {// skip ignore values; replace new lines with space...prevents string concatination
                    if(Program._newLineDelimiters.Contains(c))
                        sw.Write(" ");

                    q = 3;
                }                
                else if(q == 2 && c == ',')
                {// transition back to initial state
                    sw.Write(c);
                    q = 0;
                }                     
                else if (q == 2 && Program._newLineDelimiters.Contains(c))
                {// reached end of the line
                    sw.Write(Environment.NewLine);
                    q = 4;
                    Console.WriteLine(++index);
                }                     
                else if(q == 1 || q == 2 || q == 3)
                {// valid character write to file
                    sw.Write(c);
                    q = 1;
                }  
                else if (q == 0 || c == char.MaxValue)
                {// exit reader...
                    isEOF = true;
                }                              
            }// end while
        }// end function

        /*
        public static void CreateCleanCopyOfFile(FileStream fs, StreamWriter sw)
        {

            int index = 0;
            bool isEOF = false; // is end of file
            bool isEOL = false; // is end of line
            char nl = char.MaxValue; // indicates we've read first character of new line

            while (isEOF == false)
            {
                isEOL = false;
                
                if (nl != char.MaxValue)
                {
                    sw.Write(nl);
                    nl = char.MaxValue;
                }

                while (isEOL == false)
                {// read each line
                    char c = (char)fs.ReadByte();

                    // check if file is empty
                    if (c == char.MaxValue || c == '\0')
                    {
                        isEOF = true;
                        break;
                    }
                    
                    if (c == '"')
                    {// check what to do with quote
                        sw.Write(c);
                        c = (char)fs.ReadByte();

                        if (c == ',')
                        {// if it's followed by comma append and read next byte
                            sw.Write(c);
                            c = (char)fs.ReadByte();
                        }
                        else if (Program._newLineDelimiters.Contains(c)) //  c == '\r' || c == '\n')
                        {// if quote is followed by 
                         // new line assume it's end of line
                            isEOL = true;
                            break;
                        }
                    }

                    while (true)
                    {
                        if (c == char.MaxValue || c == '\0')
                        {// if reading empty bytes exit out
                            break;
                        }

                        // check if it's proper closing quote
                        if (c == '"')
                        {// add sub-item character to value
                            sw.Write(c);

                            // looking ,
                            c = (char)fs.ReadByte();

                            if (c == ',')
                            {
                                // write current character to new file
                                sw.Write(c);
                                
                                // read next
                                c = (char)fs.ReadByte();

                                if (c == '"')
                                {// if next character is quote then we've closed out quote set; exit loop
                                    sw.Write(c);
                                    break;
                                }// we've met exit criteria
                                else
                                {// it's a quote inside quotes, continue reading
                                    sw.Write(c);
                                }
                            }
                            else if (Program._newLineDelimiters.Contains(c))//c == '\n')
                            {                                
                                c = (char)fs.ReadByte();

                                if (Program._newLineDelimiters.Contains(c))//c == '\r')
                                {
                                    isEOL = true;
                                    break; // end of line reached
                                }
                                else if (c == '"')
                                {// reading byte of next line
                                    isEOL = true;
                                    nl = c; // memorize first character of next line
                                    break; // end of line reached; exit loop
                                }
                            }
                            else
                            {
                                sw.Write(c);
                                c = (char)fs.ReadByte();
                            }
                        }
                        // ignore new lines inside quotes
                        else if (Program._itemsToClean.Contains(c))//c == '\r' || c == '\n')
                        {// ignore CR LF inside quotes
                            c = (char)fs.ReadByte();
                        }
                        else
                        {// acceptable character received; copy and continue reading
                            sw.Write(c);
                            c = (char)fs.ReadByte();
                        }
                    }
                }

                // close the line
                sw.Write(Environment.NewLine);
                Console.WriteLine(++index);      
                          
            }// file while
        }// function
        */ 
    }
}
