using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NewSnap.Lib
{
    public static class SaveReader
    {
        public static readonly IReadOnlyList<string> SaveFileNames = new[]
        {
            "5652712535912f19",
            "398ecf2b56c614e3",
            "6706e7d0f3dab2ab",
            "00c31e53dde3ac51",
            "4f342de95be7450e",
            "0e7280b1c3ce5a0c",
            "04289da492638279",
            "9c1c815c54797c18",
            "bf0b4b223f011bf9",
            "4b1a0e859a3f7816",
            "b8b0ce894762e7a8",
            "76401e93e6565bd5",
            "0a980178a54e26f3",
            "4207cfe275441231",
            "75006a27cf0c9275",
            "9661edcf41d19929",
        };

        public static readonly IReadOnlyList<uint> SaveFileKeys = new[]
        {
            0xB519FC35,
            0xB519FC36,
            0xB519FC37,
            0xB519FC38,
            0xB519FC39,
            0xB519FC3A,
            0xB519FC3B,
            0xB519FC3C,
            0xB519FC3D,
            0xB519FC3E,
            0xB519FC3F,
            0xB519FC40,
            0xB519FC41,
            0xB519FC42,
            0xB519FC43,
            0xB519FC44,
        };

        public static bool IsCompleteSaveDirectory(string dir) => Directory.EnumerateFiles(dir).All(SaveFileNames.Contains);

        public static readonly byte[] HeaderKey =
        {
            0x1F, 0xC5, 0xD5, 0x71, 0xBD, 0xEF, 0xAF, 0x83, 0xFC, 0x96, 0xEE, 0xFE, 0x70, 0xA1, 0x14, 0xEC
        };

        /// <summary>
        /// Gets a <see cref="SaveFileHeader"/> from the input save file's raw header data.
        /// </summary>
        /// <param name="header">Header data</param>
        /// <param name="index">Save File Index</param>
        public static SaveFileHeader GetSaveFileHeader(ReadOnlySpan<byte> header, int index)
        {
            var seed = SaveFileKeys[index];
            var key = HeaderKey;

            var decHeader = DecryptHeader(header, seed, key);
            return new SaveFileHeader(decHeader);
        }

        /// <summary>
        /// Gets a <see cref="SaveFileEntry"/> from the input save file's raw entry data.
        /// </summary>
        /// <param name="entry">Entry data</param>
        /// <param name="index">Save File Index</param>
        /// <param name="entryIndex">File Index within Save File</param>
        public static SaveFileEntry GetSaveFileEntry(ReadOnlySpan<byte> entry, int index, int entryIndex)
        {
            var seed = SaveFileKeys[index];
            var key = HeaderKey;

            var decEntry = DecryptEntry(entry, seed + (uint)entryIndex, key);
            return new SaveFileEntry(decEntry);
        }

        public static byte[] DecryptHeader(ReadOnlySpan<byte> header, uint seed, byte[] key)
        {
            var dat = header[..0x20];
            var encIV = header[0x20..0x30];

            var iv = ReadDecryptedIV(encIV, seed);
            return Decrypt(key, iv, dat);
        }

        private static byte[] DecryptEntry(ReadOnlySpan<byte> entry, uint seed, byte[] key)
        {
            var encIV = entry[..0x10];
            var dat = entry[0x10..];

            var iv = ReadDecryptedIV(encIV, seed);
            return Decrypt(key, iv, dat);
        }

        private static byte[] Decrypt(byte[] key, byte[] iv, ReadOnlySpan<byte> dat)
        {
            using var ms = new MemoryStream(dat.Length);
            ms.Write(dat);
            ms.Position = 0;

            using var aes = new AesManaged
            {
                Key = key,
                IV = iv,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.None
            };
            using var decryptor = aes.CreateDecryptor();
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

            var decrypted = new byte[dat.Length];
            cs.Read(decrypted);
            return decrypted;
        }

        private static byte[] ReadDecryptedIV(ReadOnlySpan<byte> entry, uint seed)
        {
            var rng = new XorShift(seed);
            var iv = new byte[0x10];
            for (var i = 0x0; i < iv.Length; ++i)
                iv[i] = (byte)(entry[i] ^ (rng.GetNext(0xFE) + 1));
            return iv;
        }

        public static int GetIndex(string fileName)
        {
            const ulong lo = 0x2163A6B38429BC22UL;
            const ulong hi = 0x9B4923E9F5AAB470UL;
            var rng = new XorShift(lo, hi);
            for (int i = 0; i < 100; i++)
            {
                var first = rng.GetNext(uint.MaxValue);
                var second = rng.GetNext(uint.MaxValue);
                var fn = $"{first:x8}{second:x8}";
                if (fileName == fn)
                    return i;
            }
            return -1;
        }
    }
}
