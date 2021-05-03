using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NewSnap.Lib;
using Xunit;

namespace NewSnap.Tests
{
    public class DrpTests
    {
        private const string path = @"E:\archive.drp";

        [Fact]
        public void VerifyChecksumsAndRebuild()
        {
            var original = File.ReadAllBytes(path);
            var data = (byte[]) original.Clone();
            (data.Length % 4).Should().Be(0);
            var drp = new DrpArchive(data);
            drp.FileCount.Should().NotBe(0);

            var mainSeed = BitConverter.ToUInt32(data, 0x04);
            var mainChk = DrpArchiveWriter.GetHeaderSeed(drp);
            mainSeed.Should().Be(mainChk);

            var seed = BitConverter.ToUInt32(data, 0x94);
            var chk = Crc32.ComputeChecksum(drp.Files[0].GetData());
            seed.Should().Be(chk);

            using var ms = new MemoryStream(original.Length);
            drp.Write(ms);
            var result = ms.ToArray();
            result.Length.Should().Be(original.Length);
            result.SequenceEqual(original).Should().BeTrue();
        }

        [Fact]
        public void VerifyMutate()
        {
            var original = File.ReadAllBytes(path);
            var data = (byte[])original.Clone();
            (data.Length % 4).Should().Be(0);
            var drp = new DrpArchive(data);
            drp.FileCount.Should().NotBe(0);

            // Zero out all arrays, keep original data on the side.
            var old = drp.Files.Select(z => z.GetData()).ToArray();
            for (int i = 0; i < drp.FileCount; i++)
                drp.Files[i].SetData(new byte[old[i].Length]);

            using var ms = new MemoryStream(original.Length);
            drp.Write(ms);
            var result = ms.ToArray();
            result.Length.Should().BeLessThan(original.Length); // Compression of zeroes is more compact
            result.SequenceEqual(original).Should().BeFalse(); // Different data ^

            // Restore original data to modified drp, verify matches original drp
            drp = new DrpArchive(result);

            for (int i = 0; i < drp.FileCount; i++)
                drp.Files[i].SetData(old[i]);
            using var ms2 = new MemoryStream(original.Length);

            drp.Write(ms2);
            var result2 = ms2.ToArray();
            result2.Length.Should().Be(original.Length);
            result2.SequenceEqual(original).Should().BeTrue();
        }
    }
}
