using System;
using System.IO;
using FluentAssertions;
using NewSnap.Lib;
using Xunit;

namespace NewSnap.Tests
{
    public class SaveTests
    {
        [Fact]
        public void DumpSaveNames()
        {
            const ulong lo = 0x2163A6B38429BC22UL;
            const ulong hi = 0x9B4923E9F5AAB470UL;
            var rng = new XorShift(lo, hi);
            for (int i = 0; i < 15; i++)
            {
                var first = rng.GetNext(uint.MaxValue);
                var second = rng.GetNext(uint.MaxValue);
                var fn = $"{first:x8}{second:x8}";
                var expect = SaveReader.SaveFileNames[i];
                fn.Should().Be(expect);
            }
        }

        [Fact]
        public void TestHeaderDecrypt()
        {
            var h = SaveReader.GetSaveFileHeader((byte[])Header00C_3.Clone(), 3);

            h.Magic.Should().Be(0xC63E569C);
            h.EncryptedEntrySize.Should().Be(0x70840);
            h.DecryptedEntrySize.Should().Be(0x70810);
            h.SizeMaybe.Should().Be(0x30);
        }

        [Fact]
        public void TestEntryDecrypt()
        {
            var e = SaveReader.GetSaveFileEntry((byte[])Entry000_0.Clone(), 0, 0);

            e.Magic.Should().Be(0x9B7A0B7C);
            e.AdlerChecksum.Should().Be(Adler32.ComputeChecksum(e.Data));
            e.Crc32Checksum.Should().Be(Crc32.ComputeChecksum(e.Data));
            e.DataSize.Should().Be(e.Data.Length);
            e.DataSizeDuplicate.Should().Be(e.Data.Length);
        }

        private static readonly byte[] Header00C_3 =
        {
            0x50, 0x70, 0x20, 0x84, 0x64, 0xD6, 0x19, 0xBE, 0x34, 0x93, 0xE0, 0x3B, 0x05, 0x04, 0xDD, 0xD8,
            0x98, 0xB0, 0x9B, 0xE4, 0xCF, 0xE5, 0xA1, 0x4E, 0xAC, 0xA3, 0x94, 0xA4, 0xBA, 0x71, 0x13, 0x3F,
            0xCC, 0xC6, 0xE8, 0x77, 0xA8, 0x9C, 0xCC, 0xB1, 0x94, 0xBC, 0x4F, 0xDD, 0xA7, 0x7F, 0x44, 0x00,
        };

        private static readonly byte[] Entry000_0 =
        {
            0x85, 0x11, 0xDD, 0x8E, 0xCE, 0xD4, 0x3A, 0x3B, 0x73, 0x1C, 0x49, 0x7A, 0x75, 0xEB, 0xAB, 0xEA,
            0xEF, 0x6F, 0x31, 0x54, 0x45, 0xA1, 0xD4, 0xA1, 0xF9, 0x40, 0x7E, 0x7E, 0x14, 0xAB, 0x62, 0x89,
            0xA1, 0xF9, 0x41, 0xE6, 0x57, 0xB5, 0x9D, 0xCC, 0x8B, 0xA9, 0xD5, 0x0B, 0x55, 0xB1, 0x0B, 0x64,
            0xDB, 0x9B, 0xCB, 0xB4, 0x97, 0x48, 0xF3, 0x6B, 0x6C, 0xA5, 0x31, 0xC5, 0x0B, 0x7D, 0x04, 0x34,
            0xB9, 0x47, 0x7D, 0x6C, 0xA9, 0xD1, 0x2E, 0x45, 0x71, 0xC5, 0x8C, 0x79, 0x72, 0xAB, 0x30, 0x4E,
            0xCC, 0x0A, 0x42, 0x2A, 0x47, 0xC9, 0x2A, 0xDE, 0x63, 0x86, 0x5F, 0x44, 0x95, 0xC0, 0x2D, 0xCE,
            0x9B, 0xAC, 0x99, 0xE6, 0xA6, 0x6B, 0x1F, 0xA2, 0x56, 0x10, 0x8F, 0x5A, 0xEB, 0xA9, 0xE9, 0x47,
            0x75, 0x64, 0x61, 0xF8, 0x78, 0x22, 0xA9, 0x20, 0x21, 0x12, 0xE2, 0x29, 0x0E, 0x08, 0x78, 0xD5,
            0x3E, 0x56, 0x9C, 0xF2, 0x7B, 0x36, 0xF8, 0x89, 0x5E, 0x72, 0x9D, 0x88, 0xCC, 0x0B, 0x4E, 0x65,
        };

        [Fact]
        public void TestDecryptFull()
        {
            const string dir = @"E:\snap\save\";
            for (var index = 0; index < 16; ++index)
            {
                var name = SaveReader.SaveFileNames[index];
                var file = Path.Combine(dir, name);
                if (!File.Exists(file))
                    continue;

                var encsave = File.ReadAllBytes(file);

                var h = SaveReader.GetSaveFileHeader(new ReadOnlySpan<byte>(encsave, 0, 0x30), index);

                var decsave = new byte[0x20 + (h.EntryCount * h.DecryptedEntrySize)];
                var encHeader = encsave.AsSpan(..0x30);
                SaveReader.DecryptHeader(encHeader, SaveReader.SaveFileKeys[index], SaveReader.HeaderKey).CopyTo(decsave, 0);

                for (var i = 0; i < h.EntryCount; ++i)
                {
                    var start = 0x30 + (i * h.EncryptedEntrySize);
                    if (BitConverter.ToUInt64(encsave, start) == 0)
                        continue;

                    var encEntry = encsave.AsSpan(start, h.EncryptedEntrySize);

                    var e = SaveReader.GetSaveFileEntry(encEntry, index, i);

                    var region = e.Data.AsSpan(..h.DecryptedEntrySize);

                    e.AdlerChecksum.Should().Be(Adler32.ComputeChecksum(region));
                    e.Crc32Checksum.Should().Be(Crc32.ComputeChecksum(region));
                    e.Magic.Should().Be(0x9B7A0B7C);
                    e.DataSize.Should().BeLessOrEqualTo(e.Data.Length);
                    e.DataSizeDuplicate.Should().BeLessOrEqualTo(e.Data.Length);
                    e.Data.Length.Should().Be((h.DecryptedEntrySize + 15) & ~15);

                    var decDest = decsave.AsSpan(0x20 + (i * h.DecryptedEntrySize));
                    region.CopyTo(decDest);
                }

                var dest = Path.Combine(dir, $"{name}.dec");
                File.WriteAllBytes(dest, decsave);
            }
        }

        [Fact]
        public void TestExtractFull()
        {
            const string srcDir = @"E:\snap\save\";
            SaveDumper.ExtractEntries(srcDir, srcDir);
        }
    }
}
