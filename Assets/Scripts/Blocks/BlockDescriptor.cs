using System.Collections.Generic;
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

        #region Specific fields for behaviour lookup, to be filled in the constructor
        // not pretty, but we need fast access time, and a lookup table would require a cast in the end
        public readonly FireSpread FireSpread;
        public readonly Despawn Despawn;
        public readonly Swap Swap;
        #endregion

        public BlockDescriptor(string name,
            BlockTags tag,
            float densityPriority,
            Color color,
            float colorMaxShift,
            float combustionProbability,
            float baseHealth,
            int initialStates,
            IEnumerable<IBehavior> behaviors)
        {
            Name = name;
            Tag = tag;
            DensityPriority = densityPriority;
            Color = color;
            ColorMaxShift = colorMaxShift;
            CombustionProbability = combustionProbability;
            BaseHealth = baseHealth;
            InitialStates = initialStates;

            FireSpread = null;
            Despawn = null;
            Swap = null;

            foreach (var behavior in behaviors)
            {
                switch (behavior)
                {
                    case FireSpread fireSpread:
                        FireSpread = fireSpread;
                        break;
                    case Despawn despawn:
                        Despawn = despawn;
                        break;
                    case Swap swap:
                        Swap = swap;
                        break;
                }
            }
        }
    }
}
