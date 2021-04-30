using System;

namespace NewSnap.Lib
{
    public class SaveFileHeader
    {
        public uint Magic;
        public int EncryptedEntrySize;
        public int DecryptedEntrySize;
        public int EntryCount;
        public int SizeMaybe;
        public uint Unk14;
        public uint Unk18;
        public uint Unk1C;

        public SaveFileHeader(ReadOnlySpan<byte> decHeader)
        {
            Magic = BitConverter.ToUInt32(decHeader);
            EncryptedEntrySize = BitConverter.ToInt32(decHeader[4..]);
            DecryptedEntrySize = BitConverter.ToInt32(decHeader[8..]);
            EntryCount = BitConverter.ToInt32(decHeader[0xC..]);
            SizeMaybe = BitConverter.ToInt32(decHeader[0x10..]);

            Unk14 = BitConverter.ToUInt32(decHeader[0x14..]);
            Unk18 = BitConverter.ToUInt32(decHeader[0x18..]);
            Unk1C = BitConverter.ToUInt32(decHeader[0x1C..]);
        }
    }
}
