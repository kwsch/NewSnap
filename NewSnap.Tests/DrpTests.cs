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
        const string path = @"E:\archive.drp";

        [Fact]
        public void TryCalcChecksum()
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
            File.WriteAllBytes(@"E:\created.drp", result);
            result.Length.Should().Be(original.Length);
            result.SequenceEqual(original).Should().BeTrue();
        }
    }
}
