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
        public readonly float BaseHealth;
        public readonly int InitialStates;

        #region Specific fields for behaviour lookup, to be filled in the constructor
        // not pretty, but we need fast access time, and a lookup table would require a cast in the end
        public readonly Consumer Consumer;
        public readonly FireSpreader FireSpreader;
        public readonly Despawner Despawner;
        public readonly Swapper Swapper;
        #endregion

        public BlockDescriptor(string name,
            BlockTags tag,
            float densityPriority,
            Color color,
            float baseHealth,
            int initialStates,
            IEnumerable<IBehavior> behaviors)
        {
            Name = name;
            Tag = tag;
            DensityPriority = densityPriority;
            Color = color;
            BaseHealth = baseHealth;
            InitialStates = initialStates;

            Consumer = null;
            FireSpreader = null;
            Despawner = null;
            Swapper = null;

            foreach (var behavior in behaviors)
            {
                switch (behavior)
                {
                    case Consumer consume:
                        Consumer = consume;
                        break;
                    case FireSpreader fireSpread:
                        FireSpreader = fireSpread;
                        break;
                    case Despawner despawn:
                        Despawner = despawn;
                        break;
                    case Swapper swap:
                        Swapper = swap;
                        break;
                }
            }
        }
    }
}
