using System;
using System.Diagnostics;

namespace NewSnap.Lib
{
    /// <summary>
    /// 128bit XorShift RNG, seeded from a 32bit value expanded into 128bits.
    /// </summary>
    public class XorShift
    {
        private const uint Mult = 0x41C64E6D;
        private const uint Add = 12345;
        private uint _x;
        private uint _y;
        private uint _z;
        private uint _w;

        public XorShift(uint seed)
        {
            _w = (Mult * seed) + Add;
            _z = (Mult * _w) + Add;
            _y = (Mult * _z) + Add;
            _x = (Mult * _y) + Add;
        }

        public XorShift(ulong lo, ulong hi)
        {
            _w = (uint)lo;
            _z = (uint)(lo >> 32);
            _y = (uint)hi;
            _x = (uint)(hi >> 32);
        }

        public uint GetNext()
        {
            var t = _x ^ (_x << 11);
            _x = _y;
            _y = _z;
            _z = _w;
            _w = _w ^ (uint)((int)_w >> 19) ^ (t ^ (uint)((int)t >> 8));
            return _w & 0x7FFFFFFF;
        }

        /// <summary>
        /// Gets the next random number.
        /// </summary>
        /// <param name="limit">Inclusive maximum?</param>
        /// <returns></returns>
        public uint GetNext(uint limit) => limit switch
        {
            0 => 0,
            0x7FFFFFFF => GetNext(),
            >= 0x80000000 => GetNextUnsigned(limit),
            _ => GetNextWithin(limit)
        };

        private uint GetNextWithin(uint limit)
        {
            Debug.Assert(limit < int.MaxValue);
            var divisor = 0x80000000 / (limit + 1);
            while (true)
            {
                var result = GetNext() / divisor;
                if (result <= limit)
                    return result;
            }
        }

        private uint GetNextUnsigned(uint limit)
        {
            Debug.Assert(limit > int.MaxValue);
            // This method is inlined and can't be called without a high enough limit.
            // Some of the disassembly optimizations / modifications depend on this.

            const uint MaxLimit = 0xFFFFFFFF;
            const int MaxLimitSigned = int.MaxValue;

            // There's a little bit of optimization to get the result via multiple calls.
            var maximum = limit + 1;
            var passes = (limit == MaxLimit) ? 2u : 1u; // if max, 2, otherwise (x+1) >> 31, which is always 1 for our inlined logic.
            var result = 0u;
            var carry = 1u;
            while (true)
            {
                result += GetNext() * carry;

                // Edge case for limit = 0x80000000 and 0x80000001
                // 0x7FFFFFFF * 0x00000001 == 0x80000000 - 1 (first iteration)
                // 0x7FFFFFFF * 0x80000000 == 0x80000001 - 1 (second iteration)
                if (MaxLimitSigned * carry == maximum - carry)
                    return result;

                // This check is kinda useless with the inlined logic, since it is never 0.
                carry <<= 31;
                if (carry <= passes)
                    continue;

                var next = GetNext(limit / carry);
                if (next <= MaxLimit / carry)
                {
                    var res = result + (next * carry);
                    if (res >= result && res <= limit)
                        return res;
                }

                // Went over our limit! Try again.
                result = 0;
                carry = 1;
            }
        }
    }

    public static class XorShiftUtil
    {
        public static void DecryptWord(this XorShift rng, Span<byte> archive)
        {
            uint rand = rng.GetNext(0xFFFFFFFF);
            archive[0] ^= (byte)(rand >> 00);
            archive[1] ^= (byte)(rand >> 08);
            archive[2] ^= (byte)(rand >> 16);
            archive[3] ^= (byte)(rand >> 24);
        }

        public static void DecryptWord(this XorShift rng, Span<uint> data, int index) => data[index] ^= rng.GetNext(0xFFFFFFFF);

        public static uint GetXorshiftSeed(uint seed, ReadOnlySpan<byte> table)
        {
            var key = 0u;
            for (int i = 0; i < 4; ++i)
            {
                var index = (int)((seed >> (i * 8)) & 0x7F);
                key |= (uint)table[index] << (i * 8);
            }

            return key;
        }

        public static XorShift GetEncryptionRng(uint seed, ReadOnlySpan<byte> table) => new(GetXorshiftSeed(seed, table));
    }
}
