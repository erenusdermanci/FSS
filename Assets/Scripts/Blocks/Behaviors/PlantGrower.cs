
namespace Blocks.Behaviors
{
    public class PlantGrower : IBehavior
    {
        public readonly float XInitialGrowthDirection;
        public readonly float YInitialGrowthDirection;
        public readonly int MaximumDepthLevels;
        private readonly int _minimumTicksBeforeGrowth;
        private readonly int _maximumTickBeforeGrowth;
        private readonly float _branchProbability;
        private readonly float[] _growthDirectionVariationProbabilitiesPerDepthLevel;
        private readonly int[] _minimumDistancesPerDepthLevel;
        private readonly int[] _maximumDistancesPerDepthLevel;
        private readonly int[] _growthMediumBlocks;
        private readonly int _rootType;

        public PlantGrower(float xInitialGrowthDirection,
            float yInitialGrowthDirection,
            int maximumDepthLevels,
            float branchProbability,
            float[] growthDirectionVariationProbabilitiesPerDepthLevel,
            int[] minimumDistancesPerDepthLevel,
            int[] maximumDistancesPerDepthLevel,
            int minimumTicksBeforeGrowth,
            int maximumTicksBeforeGrowth,
            int[] growthMediumBlocks,
            int rootType)
        {
            XInitialGrowthDirection = xInitialGrowthDirection;
            YInitialGrowthDirection = yInitialGrowthDirection;
            MaximumDepthLevels = maximumDepthLevels;
            _branchProbability = branchProbability;
            _growthDirectionVariationProbabilitiesPerDepthLevel = growthDirectionVariationProbabilitiesPerDepthLevel;
            _minimumDistancesPerDepthLevel = minimumDistancesPerDepthLevel;
            _maximumDistancesPerDepthLevel = maximumDistancesPerDepthLevel;
            _minimumTicksBeforeGrowth = minimumTicksBeforeGrowth;
            _maximumTickBeforeGrowth = maximumTicksBeforeGrowth;
            _growthMediumBlocks = growthMediumBlocks;
            _rootType = rootType;
        }
    }
}
