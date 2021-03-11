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
    
    public enum Blocksosef
    {
        /* 0 */ Air,
        /* 1 */ Cloud,
        /* 2 */ Oil,
        /* 3 */ Water,
        /* 4 */ Sand,
        /* 5 */ Dirt,
        /* 6 */ Stone,
        /* 7 */ Metal,
        /* 8 */ Border
    }

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
    
    public class BlockLogic
    {
        public Tags PhysicalTag;
        public Tags[] BehavioralTags;
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
        public Tags[] BlockedBy;
    }

    public class Blocks
    {
        public static BlockLogic[] BlockDescriptors = {
            new() { // AIR
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Behaviors = new IBehavior[] {}
            },
            new() { // CLOUD
                PhysicalTag = NonPhysical,
                BehavioralTags = new Tags[] {},
                Behaviors = new IBehavior[] {}
            },
            // directions
            // 0 -> down
            // 1 -> downLeft
            // 2 -> downRight
            // 3 -> left
            // 4 -> right
            // 5 -> up
            // 6 -> upLeft
            // 7 -> upRight
            new() { // OIL
                PhysicalTag = Liquid,
                BehavioralTags = new[] { Liquid },
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        Priorities = new [] { 0, 1, 1, 2 },
                        Directions = new [] { 5, 4, 6, 7 },
                        MovementTypes = new[] { Randomized, Randomized, Randomized, Randomized },
                        BlockedBy = new[] { Solid } // check physicalTag
                    }
                }
            },
            new() { // WATER
                PhysicalTag = Liquid,
                BehavioralTags = new [] { Liquid, Conductive },
                Behaviors = new IBehavior[]
                {
                    new Swap
                    {
                        // For priorities -1 means no movement in this direction
                        Priorities = new [] { 0, 1, 1, 2, 2, -1, -1, -1 },
                        // For directions 0 means no movement
                        Directions = new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        MovementTypes = new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockedBy = new[] { Solid } // check physicalTag
                    }
                }
            }
        };
    }
}