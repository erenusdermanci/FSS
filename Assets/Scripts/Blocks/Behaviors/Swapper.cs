using System;

namespace Blocks.Behaviors
{
    public class Swapper : IBehavior
    {
        private readonly int[] _priorities;
        private readonly int[] _directions;
        private readonly BlockMovementType[] _movementTypes;
        private readonly bool[] _blockedBy;

        public Swapper(int[] priorities, int[] directions, BlockMovementType[] movementTypes, BlockTags[] blockedBy)
        {
            _priorities = priorities;
            _directions = directions;
            _movementTypes = movementTypes;
            _blockedBy = new bool[Enum.GetValues(typeof(BlockTags)).Length];
            foreach (var blockTag in blockedBy)
            {
                _blockedBy[(int) blockTag] = true;
            }
        }
    }
}
