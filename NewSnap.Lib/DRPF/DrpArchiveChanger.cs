using System;
using System.Collections.Generic;
using System.IO;

namespace NewSnap.Lib
{
    public static class DrpArchiveChanger
    {
        /// <summary>
        /// Injects the <see cref="file"/> into the <see cref="source"/> and updates the saved archive.
        /// </summary>
        public static void Replace(string source, string file) => Replace(source, source, new[] {file});

        public static void Replace(string source, string destination, string replace)
        {
            var files = Directory.EnumerateFiles(replace);
            Replace(source, destination, files);
        }

        public static void Replace(string source, string destination, IEnumerable<string> filesToReplace)
        {
            var drp = Replace(source, filesToReplace);
            using var fs = File.Open(destination, FileMode.Create, FileAccess.Write);
            drp.Write(fs);
        }

        private static DrpArchive Replace(string source, IEnumerable<string> filesToReplace)
        {
            var data = File.ReadAllBytes(source);
            var drp = new DrpArchive(data);
            ReplaceFilesInArchive(drp, filesToReplace);
            return drp;
        }

        private static void ReplaceFilesInArchive(DrpArchive drp, IEnumerable<string> filesToReplace)
        {
            foreach (var f in filesToReplace)
                ReplaceFile(drp, f);
        }

        private static void ReplaceFile(DrpArchive drp, string fileFullPath)
        {
            var fn = Path.GetFileName(fileFullPath);
            ReplaceFileName(drp, fileFullPath, fn);
        }

        private static void ReplaceFileName(DrpArchive drp, string fileFullPath, string fn)
        {
            var entry = drp.GetFileEntry(fn);
            if (entry is not null)
                ReplaceEntry(entry, fileFullPath);
            else
                Console.WriteLine($"Unable to find file in the input drp archive: {fn}");
        }

        private static void ReplaceEntry(DrpFileEntry entry, string fileFullPath)
        {
            var replaceWith = File.ReadAllBytes(fileFullPath);
            ReplaceEntry(entry, replaceWith);
        }

        private static void ReplaceEntry(DrpFileEntry entry, byte[] replaceWith)
        {
            var before = entry.GetData().Length;
            var after = replaceWith.Length;
            entry.SetData(replaceWith);

            var msg = $"Replaced: {entry.FileName}.";
            if (before != after)
                msg += $" {before}->{after}";
            Console.WriteLine(msg);
        }
    }
}
