using UnityEngine;
using static BlockBehavior.MovementType;
using static BlockBehavior.Tags;

namespace BlockBehavior
{
    // directions
    // 0 -> down
    // 1 -> downLeft
    // 2 -> downRight
    // 3 -> left
    // 4 -> right
    // 5 -> up
    // 6 -> upLeft
    // 7 -> upRight

    public enum Tags
    {
        Liquid,
        Solid,
        Conductive,
        NonPhysical
    }
    
    public interface IBehavior
    {
        int Id { get; }
    }
    
    public readonly struct BlockDescriptor
    {
        public readonly Tags PhysicalTag;
        public readonly Tags[] BehavioralTags;
        public readonly float Density;
        public readonly Color32 Color;
        public readonly float ColorMaxShift;
        public readonly IBehavior[] Behaviors;

        public BlockDescriptor(Tags physicalTag, Tags[] behavioralTags, float density, Color32 color, float colorMaxShift, IBehavior[] behaviors)
        {
            PhysicalTag = physicalTag;
            BehavioralTags = behavioralTags;
            Density = density;
            Color = color;
            ColorMaxShift = colorMaxShift;
            Behaviors = behaviors;
        }
    }

    public enum MovementType
    {
        Closest,
        Randomized,
        Farthest
    }

    public readonly struct Swap : IBehavior
    {
        public int Id => 0;
        
        public readonly int[] Priorities;
        public readonly int[] Directions;
        public readonly MovementType[] MovementTypes;
        public readonly Tags BlockedBy;

        public Swap(int[] priorities, int[] directions, MovementType[] movementTypes, Tags blockedBy)
        {
            Priorities = priorities;
            Directions = directions;
            MovementTypes = movementTypes;
            BlockedBy = blockedBy;
        }
    }

    public static class BlockLogic
    {
        public static readonly BlockDescriptor[] BlockDescriptors = {
            new BlockDescriptor( // AIR
                NonPhysical,
                new Tags[] {},
                0.0f,
                new Color32(0, 0, 0, 0),
                0.0f,
                new IBehavior[] {}
            ),
            new BlockDescriptor( // CLOUD
                NonPhysical,
                new Tags[] {},
                0.0f,
                new Color32(193, 190, 186, 127),
                0.05f,
                new IBehavior[] {}
            ),
            new BlockDescriptor ( // OIL
                Liquid,
                new[] { Liquid },
                0.1f,
                new Color32(51, 38, 0, 255),
                0.1f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // WATER
                Liquid,
                new [] { Liquid, Conductive },
                0.2f,
                new Color32(15, 94, 156, 255),
                0.025f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // SAND
                Solid,
                new [] { Solid },
                0.5f,
                new Color32(155, 134, 69, 255),
                0.05f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // DIRT
                Solid,
                new [] { Solid },
                0.6f,
                new Color32(124, 94, 66, 255),
                0.1f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 2, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // STONE
                Solid,
                new [] { Solid },
                1.0f,
                new Color32(149, 148, 139, 255),
                0.2f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // METAL
                Solid,
                new [] { Solid },
                1.0f,
                new Color32(75, 75, 75, 255),
                0.01f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // BORDER
                Solid,
                new Tags[] { }, // no behavior
                1000.0f,
                new Color32(255, 0, 0, 255),
                0.0f,
                new IBehavior[] { }
            )
        };
    }
}