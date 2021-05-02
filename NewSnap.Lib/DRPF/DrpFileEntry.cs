using System.Collections.Generic;
using System.Diagnostics;

namespace NewSnap.Lib
{
    public class DrpFileEntry
    {
        public readonly DrpFileHeader Header;

        /// <summary>
        /// Relative path File Name
        /// </summary>
        /// <remarks>Haven't seen any subfolder usage at this time.</remarks>
        public readonly string FileName;

        /// <summary>
        /// Decrypted data for the file.
        /// </summary>
        /// <remarks>Mutable for purposes of repacking modified archives...</remarks>
        private byte[] Data;

        /// <summary>
        /// Indicates if the file name stored within the <see cref="DrpArchive"/> should keep the extension part of the string.
        /// </summary>
        /// <remarks>Some files exclude the extension chars, while others retain it.</remarks>
        public bool HasExtensionFileName => FileName.Contains('.');

        /// <summary>
        /// Checksum of the data.
        /// </summary>
        public uint GetChecksum() => Crc32.ComputeChecksum(Data);

        /// <summary>
        /// Indicates if the file is compressed when stored in a <see cref="DrpArchive"/>.
        /// </summary>
        public bool Compressed { get; set; }

        public DrpFileEntry(string fileName, byte[] data, DrpFileHeader header)
        {
            Header = header;
            FileName = fileName;
            Data = data;
        }

        /// <summary>
        /// Gets the saved file name of the entry.
        /// </summary>
        public string GetFullFileName()
        {
            var ext = GetExtensionString();
            if (FileName.EndsWith(ext))
                return FileName;
            if (HasExtensionFileName)
                return FileName;
            return FileName + ext;
        }

        private string GetExtensionString()
        {
            var magic = Header.Extension;
            if (DrpFileExtensions.TryGetValue(magic, out var extension))
                return $".{extension}";

            var result = magic.ToString("X8");
            Debug.WriteLine($"Unknown Extension CRC32: 0x{result}");
            return $".{result}";
        }

        public byte[] GetData() => Data;
        public void SetData(byte[] data) => Data = data;

        public override string ToString() => GetFullFileName();

        /// <summary>
        /// <see cref="Crc32"/> magic values used to store extension types.
        /// </summary>
        public static readonly IReadOnlyDictionary<uint, string> DrpFileExtensions = new Dictionary<uint, string>
        {
            {0x1C375F45, "txt"},
            {0xBE1C9ACB, "msbt"},
            {0xB9715ED2, "msbp"},
            {0xBB922BB1, "lm"},
            {0x5C156DBC, "nutexb"},
            {0x950C38A5, "bnk"},
            {0xCD63FEC8, "lmb"},
            {0x53076B6B, "lme"},
            {0x31C91A2C, "luo"},
            {0xA9B87C9F, "rtd"},
            {0xA76EEEC0, "shb"},
            {0x8C43BD03, "skb"},
            {0x0FB47EED, "achd"},
            {0x89E759E1, "allb"},
            {0xE42AB2FE, "cbsb"},
            {0xE165A47B, "cesb"},
            {0xB20291CC, "cutb"},
            {0x7A390130, "ecbd"},
            {0x9B70EEC4, "facb"},
            {0xF78548DD, "ldtb"},
            {0xBAF0E4F7, "lprb"},
            {0xA3D90E1B, "mdcd"},
            {0x37CD5F6B, "mdfb"},
            {0xEADAF976, "mdrp"},
            {0xE1C38F19, "misd"},
            {0x6971203F, "navb"},
            {0xA459AA15, "nvhb"},
            {0x0B548B0F, "path"},
            {0x58284CA4, "pcnb"},
            {0x01AA8159, "pdcd"},
            {0x6CD5ECCD, "pflb"},
            {0xB894D312, "pfrb"},
            {0x0972120A, "picd"},
            {0xF423150F, "pstb"},
            {0xBE2D954D, "ptsb"},
            {0x753C041E, "silb"},
            {0x97948F6D, "bfotf"},
            {0x87E7C3FC, "bfttf"},
            {0x2E9EDB6D, "efxbn"},
            {0x509961FC, "characterb"},
            {0x623D9D06, "matinstb"},
            {0xDAB89279, "numatb"},
            {0x8E6610FB, "modelb"},
            {0x0032C3E4, "nuanmb"},
            {0x81D2341C, "nuhlpb"},
            {0x236DB83A, "numshb"},
            {0x67E93703, "nusktb"},
            {0x3F86D270, "nusrcmdlb"},
            {0xC3157ABE, "courseb"},
            {0x9C5515DE, "nufxlb"},
            {0x2F6D9B0B, "nushdb"},
            {0x7E309203, "reflectb"},
            {0xE57E2E01, "paramb"},
            {0x2E641827, "stfrolb"},
            {0xE9AB884F, "genderb"},
            {0xF59A44EB, "levelb"},
            {0x62BC395A, "navmshb"},

            // Unknown: Probably long extensions. If ya know the real one, pull request!
            // {0xDC4A8177, "DC4A8177"}, // within romfs
            // {0xC63E569C, "C63E569C"}, // within savedata
            // {0xB519FC35, "B519FC35"}, // within savedata
            // {0x9B7A0B7C, "9B7A0B7C"}, // within savedata
        };
    }
}
