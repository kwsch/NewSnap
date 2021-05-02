using System.IO;
using FluentAssertions;
using NewSnap.Lib;
using Xunit;

namespace NewSnap.Tests
{
    public class DrpTests
    {
        [Fact]
        public void TryRead()
        {
            const string path = @"E:\archive.drp";
            var data = File.ReadAllBytes(path);
            var drp = new DrpArchive(data);
            drp.FileCount.Should().NotBe(0);
        }
    }
}
