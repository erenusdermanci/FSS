using System;

namespace Serialized
{
    [Serializable]
    public struct BlockData
    {
        public byte[] colors;
        public int[] types;
        public int[] states;
        public float[] healths;
        public float[] lifetimes;
        public long[] entityIds;
    }
}