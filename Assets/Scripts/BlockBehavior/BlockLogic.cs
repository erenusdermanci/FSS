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
    
    public class BlockDescriptor
    {
        public Tags PhysicalTag;
        public Tags[] BehavioralTags;
        public float Density;
        public IBehavior[] Behaviors;
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
            new BlockDescriptor { // AIR
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Density = 0.0f,
                Behaviors = new IBehavior[] {}
            },
            new BlockDescriptor { // CLOUD
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Density = 0.0f,
                Behaviors = new IBehavior[] {}
            },
            new BlockDescriptor { // OIL
                PhysicalTag = Liquid,
                BehavioralTags = new[] { Liquid },
                Density = 0.1f,
                Behaviors = new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Closest, Closest, Closest, Closest, Closest, Closest, Closest, Closest },
                        Solid // check physicalTag
                    )
                }
            },
            new BlockDescriptor { // WATER
                PhysicalTag = Liquid,
                BehavioralTags = new [] { Liquid, Conductive },
                Density = 0.2f,
                Behaviors = new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 2, 2, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            },
            new BlockDescriptor { // SAND
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 0.5f,
                Behaviors = new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            },
            new BlockDescriptor { // DIRT
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 0.6f,
                Behaviors = new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 1, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Closest, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            },
            new BlockDescriptor { // STONE
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 1.0f,
                Behaviors = new IBehavior[] { }
            },
            new BlockDescriptor { // METAL
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 1.0f,
                Behaviors = new IBehavior[] { }
            },
            new BlockDescriptor { // BORDER
                PhysicalTag = Solid,
                BehavioralTags = new Tags[] { }, // no behavior
                Density = 1000.0f,
                Behaviors = new IBehavior[] { }
            }
        };
    }
}