using System;

namespace NewSnap.Lib
{
    public class SaveFileEntry
    {
        public uint AdlerChecksum;
        public uint Crc32Checksum;
        public uint Magic;
        public int DataSize;
        public int DataSizeDuplicate;
        public uint Unk14;
        public uint Unk18;
        public uint Unk1C;

        public byte[] Data;

        public SaveFileEntry(ReadOnlySpan<byte> decEntry)
        {
            AdlerChecksum = BitConverter.ToUInt32(decEntry);
            Crc32Checksum = BitConverter.ToUInt32(decEntry[4..]);
            Magic = BitConverter.ToUInt32(decEntry[8..]);
            DataSize = BitConverter.ToInt32(decEntry[0xC..]);
            DataSizeDuplicate = BitConverter.ToInt32(decEntry[0x10..]);

            Unk14 = BitConverter.ToUInt32(decEntry[0x14..]);
            Unk18 = BitConverter.ToUInt32(decEntry[0x18..]);
            Unk1C = BitConverter.ToUInt32(decEntry[0x1C..]);

            Data = decEntry[0x20..].ToArray();
        }
    }
}
