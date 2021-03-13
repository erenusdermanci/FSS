using UnityEngine;

namespace Blocks
{
    public readonly struct BlockDescriptor
    {
        public readonly string Name;
        public readonly int Id;
        public readonly BlockTags PhysicalBlockTag;
        public readonly BlockTags[] BehavioralTags;
        public readonly float DensityPriority;
        public readonly Color32 Color;
        public readonly float ColorMaxShift;
        public readonly float CombustionProbability;
        public readonly float BaseHealth;
        public readonly IBehavior[] Behaviors;

        public BlockDescriptor(string name, int id, BlockTags physicalBlockTag, BlockTags[] behavioralTags,
            float densityPriority, Color32 color, float colorMaxShift, float combustionProbability, float baseHealth,
            IBehavior[] behaviors)
        {
            Name = name;
            Id = id;
            PhysicalBlockTag = physicalBlockTag;
            BehavioralTags = behavioralTags;
            DensityPriority = densityPriority;
            Color = color;
            ColorMaxShift = colorMaxShift;
            CombustionProbability = combustionProbability;
            BaseHealth = baseHealth;
            Behaviors = behaviors;
        }
    }
}