namespace Blocks
{
    public readonly struct Swap : IBehavior
    {
        public int Id => 0;
        
        public readonly int[] Priorities;
        public readonly int[] Directions;
        public readonly BlockMovementType[] MovementTypes;
        public readonly BlockTags BlockedBy;

        public Swap(int[] priorities, int[] directions, BlockMovementType[] movementTypes, BlockTags blockedBy)
        {
            Priorities = priorities;
            Directions = directions;
            MovementTypes = movementTypes;
            BlockedBy = blockedBy;
        }
    }
}