using System;
using System.IO;
using NewSnap.Lib;

namespace NewSnap.App
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length is not (2 or 3))
            {
                PrintUsage();
                return;
            }

            try
            {
                Dump(args);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine(ex);
            }
        }

        private static void Dump(string[] args)
        {
            var mode = args[0];
            var path = args[1];
            switch (mode)
            {
                case "-sav" when !Directory.Exists(path):
                    Console.WriteLine("Input save file directory not found.");
                    return;

                case "-sav":
                {
                    var dest = args.Length == 3 ? args[2] : path;
                    SaveDumper.ExtractEntries(path, dest);
                    break;
                }

                case "-drp" when Directory.Exists(path):
                {
                    var dest = args.Length == 3 ? args[2] : path;
                    DumpUtil.DumpAllDrp(path, dest);
                    break;
                }

                case "-drp" when !File.Exists(path):
                    Console.WriteLine("Input drp file not found.");
                    return;

                case "-drp":
                {
                    var dest = args.Length == 3 ? args[2] : Path.GetFullPath(path);
                    DumpUtil.DumpToPath(path, dest);
                    break;
                }

                default:
                    PrintUsage();
                    return;
            }

            Console.WriteLine("Done!");
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@$"{nameof(NewSnap)} Command Line
==============================

See below for command line parameters.
An optional destination path will resolve to the source file/folder's current folder if not provided.

==============================
-sav [folder] [destFolder(Optional)]
-drp [drpFile] [destFolder(Optional)]
-drp [drpFolder] [destFolder(Optional)]
==============================
Hint: [x] are string paths.
");
        }
    }
}
