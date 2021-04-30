using System.IO;
using Lib;
using Xunit;

namespace NewSnap.Tests
{
    public class MSBTTests
    {
        private const string path = @"E:\snapdump\";
        private const string outDir = @"E:\snapdump_text\";

        [Fact]
        public void DumpAllMSBT()
        {
            var files = Directory.GetFiles(path, "*.msbt", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var data = File.ReadAllBytes(f);
                var msbt = new MSBT(data);
                DumpMSBT(f, msbt);
            }
        }

        private static void DumpMSBT(string f, MSBT msbt)
        {
            var destPath = f.Replace(path, outDir);
            var dir = new FileInfo(destPath).Directory.FullName;
            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, Path.GetFileNameWithoutExtension(destPath));
            var x = msbt.GetOrderedLines();
            File.WriteAllLines(file + "_raw.txt", x);

            var y = msbt.GetOrderedLinesTab();
            File.WriteAllLines(file + "_tab.txt", y);

            var z = msbt.GetOrderedLinesSingle();
            File.WriteAllLines(file + ".txt", z);
        }
    }
}
