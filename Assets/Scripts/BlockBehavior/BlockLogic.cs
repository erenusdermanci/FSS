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

    public struct Swap : IBehavior
    {
        public int Id => 0;
        
        public int[] Priorities;
        public int[] Directions;
        public MovementType[] MovementTypes;
        public Tags BlockedBy;
    }

    public static class BlockLogic
    {
        public const int DirectionCount = 8;

        public static readonly BlockDescriptor[] BlockDescriptors = {
            new() { // AIR
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Density = 0,
                Behaviors = new IBehavior[] {}
            },
            new() { // CLOUD
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Density = 0,
                Behaviors = new IBehavior[] {}
            },
            new() { // OIL
                PhysicalTag = Liquid,
                BehavioralTags = new[] { Liquid },
                Density = 0.1f,
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        Priorities = new [] { 0, 0, 1, 2, 3, 4 },
                        Directions = new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        MovementTypes = new[] { Closest, Closest, Closest, Closest, Closest, Closest, Closest, Closest },
                        BlockedBy = Solid // check physicalTag
                    }
                }
            },
            new() { // WATER
                PhysicalTag = Liquid,
                BehavioralTags = new [] { Liquid, Conductive },
                Density = 0.2f,
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        Priorities = new [] { 0, 0, 1, 2, 3, 4 },
                        // For directions 0 means no movement
                        Directions = new [] { 4, 2, 2, 4, 4, 0, 0, 0 },
                        MovementTypes = new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockedBy = Solid // check physicalTag
                    }
                }
            },
            new() {// SAND
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 0.5f,
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        // For priorities -1 means no movement in this direction
                        Priorities = new [] { 0, 0, 1, 2 },
                        // For directions 0 means no movement
                        Directions = new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        MovementTypes = new[] { Randomized, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockedBy = Solid // check physicalTag
                    }
                }
            },
            new() {// DIRT
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 0.6f,
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        // For priorities -1 means no movement in this direction
                        Priorities = new [] { 0, 0 },
                        // For directions 0 means no movement
                        Directions = new [] { 1, 0, 0, 0, 0, 0, 0, 0 },
                        MovementTypes = new[] { Closest, Closest, Closest, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockedBy = Solid // check physicalTag
                    }
                }
            },
            new() {// STONE
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 1f,
                Behaviors = new IBehavior[] { }
            },
            new() {// METAL
                PhysicalTag = Solid,
                BehavioralTags = new [] { Solid },
                Density = 1f,
                Behaviors = new IBehavior[] { }
            },
            new() {// BORDER
                PhysicalTag = Solid,
                BehavioralTags = new Tags[] { }, // no behavior
                Density = 1000f,
                Behaviors = new IBehavior[] { }
            }
        };
    }
}