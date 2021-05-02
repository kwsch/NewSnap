using System;
using System.IO;
using NewSnap.Lib;

namespace NewSnap.App
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
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
            // Drag & Drop save file folder.
            if (args.Length == 1)
            {
                var first = args[0];
                if (Directory.Exists(first))
                {
                    Console.WriteLine("Extracting save data...");
                    SaveDumper.ExtractEntries(first, first);
                    Console.WriteLine("Done!");
                    return;
                }

                if (File.Exists(first))
                {
                    Console.WriteLine("Extracting save data...");
                    SaveDumper.ExtractFile(first);
                    Console.WriteLine("Done!");
                    return;
                }
            }

            if (args.Length is not (2 or 3 or 4))
            {
                PrintUsage();
                return;
            }

            var mode = args[0];
            var path = args[1];
            switch (mode)
            {
                case "-sav" when File.Exists(path):
                {
                    var index = SaveReader.GetIndex(Path.GetFileNameWithoutExtension(path));

                    string GetDestination()
                    {
                        var p = args.Length > 2 ? args[2] : Directory.GetParent(Path.GetFullPath(path)).FullName;
                        return Path.Combine(p, $"{index:00}");
                    }

                    var dest = GetDestination();
                    SaveDumper.ExtractFiles(path, dest, index);
                    break;
                }

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

                case "-ms" or "-mf" when !File.Exists(path):
                    Console.WriteLine("Input drp file not found.");
                    return;

                case "-ms" when args.Length > 2 && File.Exists(args[2]): // src, inj
                    DrpArchiveChanger.Replace(path, path, args[2..]);
                    break;
                case "-ms" when args.Length > 3 && File.Exists(args[3]): // src, dest, inj
                    DrpArchiveChanger.Replace(path, path, args[2..]);
                    break;

                case "-mf" when args.Length > 2 && Directory.Exists(args[2]): // src, inj
                    DrpArchiveChanger.Replace(path, path, args[2]);
                    break;
                case "-mf" when args.Length > 3 && Directory.Exists(args[3]): // src, dest, inj
                    DrpArchiveChanger.Replace(path, args[2], args[3]);
                    break;

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

Drop a save file folder onto the exe to unpack the save file's contents in the same folder.

==============================
Extract Save File:
  -sav [folder] [destFolder(Optional)]
  -sav [file] [destFolder(Optional)]

Extract Single DRPF Archive:
  -drp [drpFile] [destFolder(Optional)]

Extract Multiple DRPF Archives in Folder:
  -drp [drpFolder] [destFolder(Optional)]

Modify a DRPF with specific files:
  -ms [drpFile] [destPath(Optional)] [file1] [file2(Optional)] [file3(Optional)] [etc...]

Modify a DRPF with files in Folder:
  -mf [drpFile] [destPath(Optional)] [fileFolder]
==============================
Hint: [x] are string paths.
");

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
