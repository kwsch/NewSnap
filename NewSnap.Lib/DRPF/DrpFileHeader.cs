using System;

namespace NewSnap.Lib
{
    public class DrpFileHeader
    {
        public uint Seed;
        public uint Magic;
        public int SizeTotal;
        public uint Extension;
        public int CompressedSize;
        public int DecompressedSize;

        public DrpFileHeader(ReadOnlySpan<byte> decHeader)
        {
            Seed = BitConverter.ToUInt32(decHeader);
            Magic = BitConverter.ToUInt32(decHeader[0x04..]);
            SizeTotal = BitConverter.ToInt32(decHeader[0x8..]);
            Extension = BitConverter.ToUInt32(decHeader[0xC..]);
            CompressedSize = BitConverter.ToInt32(decHeader[0x10..]);
            DecompressedSize = BitConverter.ToInt32(decHeader[0x14..]);
        }
    }
}
