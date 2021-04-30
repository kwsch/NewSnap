using FluentAssertions;
using NewSnap.Lib;
using Xunit;

namespace NewSnap.Tests
{
    public class CrcTests
    {
        [Fact]
        public void ExtensionTests()
        {
            var dict = DrpFileEntry.DrpFileExtensions;
            foreach (var (key, ext) in dict)
                TestCrcString(ext, key);
        }

        [Theory]
        [InlineData("nutexb", 0x5C156DBC)]
        public void TestCrcString(string str, uint expect)
        {
            var crc = Crc32.ComputeChecksum(str);
            crc.Should().Be(expect);
        }
    }
}
