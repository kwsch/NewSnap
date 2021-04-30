using System;
using System.Text;

namespace NewSnap.Lib
{
    public class DrpArchive
    {
        /// <summary> Crc32(DRPF) </summary>
        private const uint ArchiveHeaderMagic = 0x7F0E5359;
        /// <summary> Crc32(fhdr) </summary>
        private const uint CryptoBlockMagic = 0xC65753E8;
        /// <summary> Crc32(resd) </summary>
        private const uint FileBlockMagic = 0xE0A331B4;
        /// <summary> Crc32(Oodl) </summary>
        private const uint CompressedDataMagic = 0xE42D98BA;

        private readonly DrpFileEntry[] _files;
        private readonly byte[] _seedTable = new byte[0x80];

        public int FileCount => _files.Length;

        public string GetFileName(int i) => _files[i].GetFullFileName();
        public byte[] GetFileData(int i) => _files[i].Data;

        public DrpArchive(byte[] archive)
        {
            // Check the archive magic.
            if (BitConverter.ToUInt32(archive, 0) != ArchiveHeaderMagic)
                throw new ArgumentException("Invalid archive magic");

            // Copy the encryption table.
            Buffer.BlockCopy(archive, 0x14, _seedTable, 0, _seedTable.Length);

            // Decrypt the archive.
            if (BitConverter.ToUInt32(archive, 8) != CryptoBlockMagic)
                Decrypt(archive);

            // Verify archive header checksum (CRC32 over file count and seed table).
            if (BitConverter.ToUInt32(archive, 4) != Crc32.ComputeChecksum(archive, 0x10, 0x84))
                throw new ArgumentException("Invalid archive checksum");

            // Get the file count.
            var fileCount = BitConverter.ToInt32(archive, 0x10);
            _files = new DrpFileEntry[fileCount];

            // Extract all files.
            var offset = 0x94;
            for (var i = 0; i < fileCount; ++i)
            {
                // Check the block magic.
                if (BitConverter.ToUInt32(archive, offset + 4) != FileBlockMagic)
                    throw new ArgumentException("Invalid File Block Magic?");

                // Get the chunk size.
                var chunkSize = BitConverter.ToInt32(archive, offset + 8);

                // Read the data from the chunk
                _files[i] = GetFile(archive, offset, chunkSize);

                // Advance to the next file.
                offset += chunkSize;
            }
        }

        private void Decrypt(byte[] archive)
        {
            // Decrypt the crypto chunk (skipping data, as the crypto table isn't encrypted).
            DecryptHeader(archive);

            // Verify the crypto table magic
            if (BitConverter.ToUInt32(archive, 8) != CryptoBlockMagic)
                throw new ArgumentException("Invalid Crypto Block Magic!");

            // Verify the crypto table size
            // TODO: There's logic for the case when this isn't 0x90 -- is this ever used?
            if (BitConverter.ToUInt32(archive, 12) != 0x90)
                throw new ArgumentException("Invalid Crypto Block Size!");

            // TODO: Verify crypto block checksum

            // Get the file count.
            var fileCount = BitConverter.ToInt32(archive, 16);

            // Decrypt each file block.
            var offset = 0x94;
            for (var i = 0; i < fileCount; ++i)
            {
                // Decrypt the current chunk.
                DecryptChunk(archive, offset);

                // Check the chunk magic is what we expect.
                // TODO: Is this guaranteed for all chunks in all archives?
                if (BitConverter.ToUInt32(archive, offset + 4) != FileBlockMagic)
                    throw new ArgumentException("Invalid File Block Magic?");

                // Advance
                var chunkSize = BitConverter.ToInt32(archive, offset + 8);
                offset += chunkSize;
            }

            // Decrypt footer.
            DecryptFooter(archive, offset);
        }

        private void DecryptHeader(byte[] archive)
        {
            // Get the RNG
            var seed = BitConverter.ToUInt32(archive, 4);
            var rng = GetEncryptionRng(seed);

            // Decrypt the header
            rng.DecryptWord(archive, 8);
            rng.DecryptWord(archive, 12);

            // Decrypt the file count.
            rng.DecryptWord(archive, 16);
        }

        private void DecryptFooter(byte[] archive, int offset)
        {
            // Get the RNG
            var seed = BitConverter.ToUInt32(archive, 4);
            var rng = GetEncryptionRng(seed);

            // Decrypt to end.
            while (offset < archive.Length)
            {
                rng.DecryptWord(archive, offset);
                offset += sizeof(uint);
            }
        }

        private void DecryptChunk(byte[] archive, int offset, bool decryptData = true)
        {
            // Get the RNG
            var seed = BitConverter.ToUInt32(archive, offset);
            var rng = GetEncryptionRng(seed);

            // Decrypt the chunk header
            rng.DecryptWord(archive, offset + 4);
            rng.DecryptWord(archive, offset + 8);

            if (!decryptData)
                return;

            // Decrypt the data
            var chunkSize = BitConverter.ToUInt32(archive, offset + 8);
            if ((chunkSize & 3) != 0)
                throw new ArgumentException($"Invalid ChunkSize {chunkSize:X} at offset {offset:X}");

            for (var i = 12; i < chunkSize; i += sizeof(uint))
                rng.DecryptWord(archive, offset + i);
        }

        private XorShift GetEncryptionRng(uint seed)
        {
            var xs = GetXorshiftSeed(seed);
            return new XorShift(xs);
        }

        /// <summary>
        /// The <see cref="seed"/> is interpreted as u8 indexes in the <see cref="_seedTable"/> to build the actual <see cref="XorShift"/> seed.
        /// </summary>
        private uint GetXorshiftSeed(uint seed)
        {
            var key = 0u;
            for (var i = 0; i < 4; ++i)
            {
                var index = (seed >> (i * 8)) & 0x7F;
                key |= (uint)_seedTable[index] << (i * 8);
            }
            return key;
        }

        private static DrpFileEntry GetFile(byte[] arc, int offset, int chunkSize)
        {
          //var fileBlockMagic = BitConverter.ToInt32(arc, offset + 0x04);
          //var chunkSize = BitConverter.ToInt32(arc, offset + 0x08);
            var extension = BitConverter.ToUInt32(arc, offset + 0x0C);

            // Get the file sizes.
            var compressedSize = BitConverter.ToInt32(arc, offset + 0x10);
            var decompressedSize = BitConverter.ToInt32(arc, offset + 0x14);

            // Get the file name length.
            var fileNameLength = 0;
            while (arc[offset + 0x18 + fileNameLength] != 0)
                fileNameLength++;

            // Get the file name.
            var fileName = Encoding.ASCII.GetString(arc, offset + 0x18, fileNameLength);

            // Extract the compressed data.
            var compressedFileOffset = (0x18 + fileNameLength + 4) & ~3;
            if (((compressedFileOffset + compressedSize + 3) & ~3) > chunkSize)
                throw new ArgumentException($"Invalid chunk extents {compressedFileOffset:X} + {compressedSize:X} > {chunkSize:X}");

            var compression = BitConverter.ToUInt32(arc, offset + compressedFileOffset);
            if (compression == CompressedDataMagic)
            {
                // File is compressed.
                var data = ReadCompressed(arc, offset, compressedSize, compressedFileOffset, decompressedSize);
                return new DrpFileEntry(fileName, data, extension) {Compressed = true};
            }
            else
            {
                // File is uncompressed.
                if (compressedSize != decompressedSize)
                    throw new ArgumentException("Invalid uncompressed file extents");

                var data = ReadDecompressed(arc, offset, compressedSize, compressedFileOffset);
                return new DrpFileEntry(fileName, data, extension) {Compressed = false};
            }
        }

        private static byte[] ReadDecompressed(byte[] arc, int offset, int compressedSize, int compressedFileOffset)
        {
            var result = new byte[compressedSize];
            Buffer.BlockCopy(arc, offset + compressedFileOffset, result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// Reads compressed data and decompresses it.
        /// </summary>
        /// <param name="arc">Full Decrypted archive data</param>
        /// <param name="offset">Start of the file's metadata</param>
        /// <param name="compressedSize">Length of compressed data</param>
        /// <param name="compressedFileOffset">Offset where the compressed data begins</param>
        /// <param name="decompressedSize">Length of data once decompressed.</param>
        /// <returns>Decompressed data</returns>
        private static byte[] ReadCompressed(byte[] arc, int offset, int compressedSize, int compressedFileOffset, int decompressedSize)
        {
            var compressed = new byte[compressedSize - 4];
            Buffer.BlockCopy(arc, offset + compressedFileOffset + 4, compressed, 0, compressed.Length);

            // Decompress the file.
            var decompressed = Oodle.Decompress(compressed, decompressedSize);
            if (decompressed == null)
                throw new ArgumentException("Failed to decompress file contents.");
            return decompressed;
        }
    }
}
