using System;

namespace NewSnap.Lib
{
    public class DrpFileHeader
    {
        /// <summary> Crc32 of decrypted data. </summary>
        public uint Seed;
        public uint Magic;
        public int SizeTotal;
        public uint Extension;
        public int CompressedSize;
        public int DecompressedSize;

        /// <summary> Crc32(resd) </summary>
        public const uint FileBlockMagic = 0xE0A331B4;
        /// <summary> Crc32(Oodl) </summary>
        public const uint CompressedDataMagic = 0xE42D98BA;

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
