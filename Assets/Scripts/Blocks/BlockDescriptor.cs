using Blocks.Behaviors;
using Utils;

namespace Blocks
{
    public readonly struct BlockDescriptor
    {
        public readonly string Name;
        public readonly BlockTags Tag;
        public readonly float DensityPriority; // Gases between 0 and 1, Air at 1 and others > 1
        public readonly Color Color;
        public readonly float ColorMaxShift;
        public readonly float CombustionProbability;
        public readonly float BaseHealth;
        public readonly int InitialStates;
        public readonly IBehavior[] Behaviors;

        public BlockDescriptor(string name,
            BlockTags tag,
            float densityPriority,
            Color color,
            float colorMaxShift,
            float combustionProbability,
            float baseHealth,
            int initialStates,
            IBehavior[] behaviors)
        {
            Name = name;
            Tag = tag;
            DensityPriority = densityPriority;
            Color = color;
            ColorMaxShift = colorMaxShift;
            CombustionProbability = combustionProbability;
            BaseHealth = baseHealth;
            InitialStates = initialStates;
            Behaviors = behaviors;
        }
    }
}
