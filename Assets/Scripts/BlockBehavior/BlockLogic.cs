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
        public readonly IBehavior[] Behaviors;
        public readonly Tags PhysicalTag;
        public readonly Tags[] BehavioralTags;
        public readonly float Density;

        public BlockDescriptor(Tags physicalTag, Tags[] behavioralTags, float density, IBehavior[] behaviors)
        {
            Behaviors = behaviors;
            PhysicalTag = physicalTag;
            BehavioralTags = behavioralTags;
            Density = density;
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
                new IBehavior[] {}
            ),
            new BlockDescriptor( // CLOUD
                NonPhysical,
                new Tags[] {},
                0.0f,
                new IBehavior[] {}
            ),
            new BlockDescriptor ( // OIL
                Liquid,
                new[] { Liquid },
                0.1f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Closest, Closest, Closest, Closest, Closest, Closest, Closest, Closest },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // WATER
                Liquid,
                new [] { Liquid, Conductive },
                0.2f,
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
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // DIRT
                Solid,
                new [] { Solid },
                0.6f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 1, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Closest, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // STONE
                Solid,
                new [] { Solid },
                1.0f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // METAL
                Solid,
                new [] { Solid },
                1.0f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // BORDER
                Solid,
                new Tags[] { }, // no behavior
                1000.0f,
                new IBehavior[] { }
            )
        };
    }
}