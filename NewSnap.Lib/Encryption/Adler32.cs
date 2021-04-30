using System;

namespace NewSnap.Lib
{
    public static class Adler32
    {
        private const uint MOD_ADLER = 65521;

        public static uint ComputeChecksum(ReadOnlySpan<byte> arr, int offset, int count)
        {
            var a = 1u;
            var b = 0u;

            for (var i = 0; i < count; ++i)
            {
                a = (a + arr[offset + i]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }

        public static uint ComputeChecksum(string arr, int offset, int count)
        {
            var a = 1u;
            var b = 0u;

            for (var i = 0; i < count; ++i)
            {
                a = (a + (byte)arr[offset + i]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }

        public static uint ComputeChecksum(ReadOnlySpan<byte> arr) => ComputeChecksum(arr, 0, arr.Length);
        public static uint ComputeChecksum(string arr) => ComputeChecksum(arr, 0, arr.Length);
    }
}
