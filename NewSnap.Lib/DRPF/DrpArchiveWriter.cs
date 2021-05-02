using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NewSnap.Lib
{
    public static class DrpArchiveWriter
    {
        /// <summary>
        /// Writes the <see cref="archive"/> to the <see cref="output"/> stream.
        /// </summary>
        public static void Write(this DrpArchive archive, Stream output)
        {
            using var bw = new BinaryWriter(output);
            var seed = GetHeaderSeed(archive);
            bw.WriteHeader(archive, seed);
            bw.WriteEntries(archive);
            bw.WriteFooter(archive, seed);
        }

        private static void WriteHeader(this BinaryWriter bw, DrpArchive archive, uint seed)
        {
            bw.Write(DrpArchive.ArchiveHeaderMagic);
            bw.Write(seed);
            if (archive.Encrypted)
            {
                var rng = XorShiftUtil.GetEncryptionRng(seed, archive.SeedTable);
                bw.Write(rng.GetNext(uint.MaxValue) ^ DrpArchive.CryptoBlockMagic);
                bw.Write(rng.GetNext(uint.MaxValue) ^ 0x90u);
                bw.Write(rng.GetNext(uint.MaxValue) ^ (uint)archive.FileCount);
            }
            else
            {
                bw.Write(DrpArchive.CryptoBlockMagic);
                bw.Write(0x90u);
                bw.Write(archive.FileCount);
            }
            bw.Write(archive.SeedTable);
        }

        private static void WriteFooter(this BinaryWriter bw, DrpArchive archive, uint seed)
        {
            var asUint = MemoryMarshal.Cast<byte, uint>(archive.Footer);
            if (archive.Encrypted)
            {
                var rng = XorShiftUtil.GetEncryptionRng(seed, archive.SeedTable);
                foreach (var u32 in asUint)
                    bw.Write(u32 ^ rng.GetNext(uint.MaxValue));
            }
            else
            {
                foreach (var u32 in asUint)
                    bw.Write(u32);
            }
        }

        private static void WriteEntries(this BinaryWriter bw, DrpArchive archive)
        {
            var files = archive.Files;
            bool encrypted = archive.Encrypted;
            var table = archive.SeedTable;
            foreach (var f in files)
                WriteEntry(bw, f, encrypted, table);
        }

        private static void WriteEntry(this BinaryWriter bw, DrpFileEntry entry, bool encrypted, ReadOnlySpan<byte> table)
        {
            Debug.WriteLine($"Writing entry at 0x0{bw.BaseStream.Position:X}.");
            var h = entry.Header;
            var seed = entry.GetChecksum();
            ReadOnlySpan<byte> payload = entry.GetData();
            h.CompressedSize = h.DecompressedSize = payload.Length;
            if (entry.Compressed)
            {
                payload = Oodle.Compress(payload);
                h.CompressedSize = 4 + payload.Length;
            }

            var nameSize = (entry.FileName.Length | 3) + 1; // next multiple of 4
            var dataSize = (h.CompressedSize + 3) & ~3; // roundup multiple of 4
            h.SizeTotal = 0x18 + nameSize + dataSize;

            bw.Write(seed);
            if (encrypted)
            {
                var rng = XorShiftUtil.GetEncryptionRng(seed, table);
                bw.Write(rng.GetNext(uint.MaxValue) ^       h.Magic);
                bw.Write(rng.GetNext(uint.MaxValue) ^ (uint)h.SizeTotal);
                bw.Write(rng.GetNext(uint.MaxValue) ^       h.Extension);
                bw.Write(rng.GetNext(uint.MaxValue) ^ (uint)h.CompressedSize);
                bw.Write(rng.GetNext(uint.MaxValue) ^ (uint)h.DecompressedSize);
                Span<byte> temp = stackalloc byte[4];

                var fn = entry.FileName;
                int ctr = 0;
                foreach (var c in fn)
                {
                    temp[ctr++] = (byte)c;
                    if (ctr != 4)
                        continue;

                    bw.Write(rng.GetNext(uint.MaxValue) ^ BitConverter.ToUInt32(temp));
                    temp.Clear();
                    ctr = 0;
                }
                if (ctr == 0)
                    bw.Write(rng.GetNext(uint.MaxValue) ^ 0u);
                else
                    bw.Write(rng.GetNext(uint.MaxValue) ^ BitConverter.ToUInt32(temp));

                var asUint = MemoryMarshal.Cast<byte, uint>(payload);
                if (entry.Compressed)
                    bw.Write(rng.GetNext(uint.MaxValue) ^ DrpFileHeader.CompressedDataMagic);
                foreach (var u32 in asUint)
                    bw.Write(rng.GetNext(uint.MaxValue) ^ u32);

                var slice = payload[(asUint.Length * 4)..];
                if (slice.Length != 0)
                {
                    temp.Clear();
                    slice.CopyTo(temp);
                    bw.Write(rng.GetNext(uint.MaxValue) ^ BitConverter.ToUInt32(temp));
                }
            }
            else
            {
                bw.Write(h.Magic);
                bw.Write((uint)h.SizeTotal);
                bw.Write(h.Extension);
                bw.Write((uint)h.CompressedSize);
                bw.Write((uint)h.DecompressedSize);

                var fn = entry.FileName;
                foreach (var c in fn)
                    bw.Write((byte)c);
                var remain = fn.Length & 3;
                if (fn.Length == 0)
                {
                    bw.Write(0u);
                }
                else
                {
                    for (int i = remain; i < 4; i++)
                        bw.Write((byte)0);
                }

                if (entry.Compressed)
                    bw.Write(DrpFileHeader.CompressedDataMagic);
                bw.Write(payload);

                remain = payload.Length & 3;
                for (int i = remain; i < 4; i++)
                    bw.Write((byte)0);
            }
        }

        private static int GetSize4(int size)
        {
            var r = (size & 3);
            return size + (r == 0 ? 4 : (4 - r));
        }

        public static uint GetHeaderSeed(DrpArchive archive)
        {
            Span<byte> chkRegion = stackalloc byte[4];
            BitConverter.TryWriteBytes(chkRegion, (uint)archive.FileCount);
            var chk1 = Crc32.ComputeChecksum(chkRegion);
            return Crc32.ComputeChecksum(archive.SeedTable, chk1);
        }
    }
}
