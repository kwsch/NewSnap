using System.Runtime.InteropServices;

namespace NewSnap.Lib
{
    public static class Oodle
    {
        /// <summary>
        /// Oodle Library Path
        /// </summary>
        private const string OodleLibraryPath = "oo2core_8_win64";

        /// <summary>
        /// Oodle64 Decompression Method 
        /// </summary>
        [DllImport(OodleLibraryPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int ThreadModule);

        /// <summary>
        /// Decompresses a byte array of Oodle Compressed Data (Requires Oodle DLL)
        /// </summary>
        /// <param name="input">Input Compressed Data</param>
        /// <param name="decompressedLength">Decompressed Size</param>
        /// <returns>Resulting Array if success, otherwise null.</returns>
        public static byte[]? Decompress(byte[] input, long decompressedLength)
        {
            // Resulting decompressed Data
            byte[] result = new byte[decompressedLength];
            // Decode the data (other parameters such as callbacks not required)
            long decodedSize = OodleLZ_Decompress(input, input.Length, result, decompressedLength, 1, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            // Check did we fail
            if (decodedSize == 0)
                return null;
            // Return Result
            return result;
        }
    }
}
