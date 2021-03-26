using System;
using Blocks;

namespace Serialized
{
    [Serializable]
    public struct PlantBlockData
    {
        public long id;
        public float xGrowthDirection;
        public float yGrowthDirection;
        public int depthLevel;
        public int distanceToRoot;
        public int growthCount;
        public int ticksBeforeGrowth;

        public void Reset(int blockType, long id = 0)
        {
            var descriptor = BlockConstants.BlockDescriptors[blockType];
            if (descriptor.PlantGrower == null)
                return;
            this.id = id;
            ticksBeforeGrowth = 0;
            distanceToRoot = 0;
            xGrowthDirection = descriptor.PlantGrower.XInitialGrowthDirection;
            yGrowthDirection = descriptor.PlantGrower.YInitialGrowthDirection;
            growthCount = 0;
        }
    }
}