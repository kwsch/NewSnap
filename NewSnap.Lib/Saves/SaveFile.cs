using System;
using System.Diagnostics;
using System.IO;

namespace NewSnap.Lib
{
    public class SaveFile : IDisposable
    {
        private readonly Stream Stream;
        private readonly SaveFileHeader Header;
        private readonly int SaveIndex;

        public int EntryCount => Header.EntryCount;

        public SaveFile(Stream stream, int index)
        {
            Stream = stream;
            SaveIndex = index;
            stream.Position = 0;
            Span<byte> hdr = stackalloc byte[0x30];
            stream.Read(hdr);

            Header = SaveReader.GetSaveFileHeader(hdr, index);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                try
                {
                    Stream.Close();
                    Stream.Dispose();
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception x)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Debug.WriteLine($"Already closed? {x}");
                }
            }
            isDisposed = true;
        }

        public byte[] GetEntry(int index)
        {
            if ((uint) index >= Header.EntryCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            var size = Header.EncryptedEntrySize;
            var start = 0x30 + (index * size);

            Span<byte> encEntry = stackalloc byte[size];
            Stream.Position = start;
            Stream.Read(encEntry);

            if (BitConverter.ToUInt64(encEntry) == 0)
                return Array.Empty<byte>();

            var e = SaveReader.GetSaveFileEntry(encEntry, SaveIndex, index);
            return e.Data[..Header.DecryptedEntrySize];
        }
    }
}
