using System;
using System.Collections.Generic;

namespace Lib
{
    public class LBL1 : MSBTSection
    {
        public uint NumberOfGroups;

        public readonly List<MSBTGroup> Groups = new();
        public readonly List<MSBTLabel> Labels = new();

        public LBL1() : base(string.Empty, Array.Empty<byte>())
        {
        }
    }
}
