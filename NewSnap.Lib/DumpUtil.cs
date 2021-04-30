using System;
using System.IO;

namespace NewSnap.Lib
{
    public static class DumpUtil
    {
        private static void EnsureEndsWithDirectorySeparator(ref string path)
        {
            path = Path.Combine(path, "x");
            path = path[..^1];
        }

        public static void DumpAllDrp(string path, string outDir)
        {
            EnsureEndsWithDirectorySeparator(ref path);
            EnsureEndsWithDirectorySeparator(ref outDir);
            string[] files = Directory.GetFiles(path, "*.drp", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; i++)
            {
                var f = files[i];
                var fi = new FileInfo(f);
                var src = fi.Directory.FullName;
                var dest = src.Replace(path, outDir);

                Console.WriteLine($"{i + 1}/{files.Length}: Reading and decrypting {f}");
                DumpToPath(f, dest);
            }
        }

        public static void DumpToPath(string path, string outDir)
        {
            var arcData = File.ReadAllBytes(path);
            var archive = new DrpArchive(arcData);
            Console.WriteLine($"Dumping {archive.FileCount} files from {path}");
            var fn = Path.GetFileNameWithoutExtension(path);
            var subDir = Path.Combine(outDir, fn);
            DumpToPath(archive, subDir);
        }

        private static void DumpToPath(DrpArchive archive, string outDir)
        {
            Directory.CreateDirectory(outDir);
            for (var i = 0; i < archive.FileCount; ++i)
            {
                var name = archive.GetFileName(i);
                var dest = Path.Combine(outDir, name);
                var data = archive.GetFileData(i);
                File.WriteAllBytes(dest, data);

                Console.WriteLine($"\t{i + 1}/{archive.FileCount}: {dest}");
            }
        }
    }
}
