using System;

namespace ProceduralGeneration
{
    [Serializable]
    public struct BlockThreshold
    {
        public int type;
        public float threshold;

        public BlockThreshold(int type, float threshold)
        {
            this.type = type;
            this.threshold = threshold;
        }
    }
}