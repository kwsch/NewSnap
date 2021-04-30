using System;
using System.IO;

namespace NewSnap.Lib
{
    public static class SaveDumper
    {
        public static void ExtractEntries(string srcDir, string destDir)
        {
            var files = SaveReader.SaveFileNames;
            for (var index = 0; index < files.Count; ++index)
                ExtractEntries(srcDir, destDir, index);
        }

        private static void ExtractEntries(string srcDir, string destDir, int index)
        {
            var files = SaveReader.SaveFileNames;
            var file = Path.Combine(srcDir, files[index]);
            ExtractEntries(file, index, destDir);
        }

        private static void ExtractEntries(string file, int index, string destDir)
        {
            if (!File.Exists(file))
                return;

            var subDir = Path.Combine(destDir, $"{index:00}");
            Directory.CreateDirectory(subDir);
            Console.Write($"Extracting {subDir}...");

            var stream = File.OpenRead(file);
            using var sav = new SaveFile(stream, index);

            // For jpeg archives, there's a lot of files.
            // Sorting by filename is good, so pad with zeroes.
            var count = sav.EntryCount;
            var digits = (int) Math.Floor(Math.Log10(count) + 1);
            Console.WriteLine($" found ~{count} files.");

            for (int i = 0; i < count; i++)
            {
                var data = sav.GetEntry(i);
                if (data.Length == 0)
                    continue;

                var ext = "bin";
                if (IsJpegData(data))
                {
                    ext = "jpg";
                    data = GetJpegOnly(data, 0x10);
                }

                var fn = i.ToString().PadLeft(digits, '0');
                var dest = Path.Combine(subDir, $"{fn}.{ext}");
                File.WriteAllBytes(dest, data);
            }
        }

        private static bool IsJpegData(byte[] data)
        {
            // First 0x10 bytes are metadata we don't care about.
            if (data.Length <= 0x20)
                return false;
            if (BitConverter.ToUInt16(data, 0x10) != 0xD8FF) // SOI
                return false;
            if (BitConverter.ToUInt32(data, 0x16) != 0x4649464A) // JFIF
                return false;
            return true;
        }

        private static byte[] GetJpegOnly(byte[] data, int start)
        {
            Span<byte> end = stackalloc byte[] { 0xFF, 0xD9 };
            var x = data.AsSpan();
            var length = x.LastIndexOf(end);
            if (length == -1)
                throw new ArgumentException("No EOF found inside jpeg data!");
            return data[start..(length + 2)];
        }
    }
}
