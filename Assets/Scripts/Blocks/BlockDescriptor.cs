using UnityEngine;

namespace Blocks
{
    public readonly struct BlockDescriptor
    {
        public readonly int Id;
        public readonly BlockTags PhysicalBlockTag;
        public readonly BlockTags[] BehavioralTags;
        public readonly float Density;
        public readonly Color32 Color;
        public readonly float ColorMaxShift;
        public readonly IBehavior[] Behaviors;

        public BlockDescriptor(int id, BlockTags physicalBlockTag, BlockTags[] behavioralTags, float density, Color32 color, float colorMaxShift, IBehavior[] behaviors)
        {
            Id = id;
            PhysicalBlockTag = physicalBlockTag;
            BehavioralTags = behavioralTags;
            Density = density;
            Color = color;
            ColorMaxShift = colorMaxShift;
            Behaviors = behaviors;
        }
    }
}