using System;
using Chunks;

namespace Blocks.Behaviors
{
    public readonly struct Swap : IBehavior
    {
        public const int Id = 0;

        public int GetId => Id;

        private readonly int[] _priorities;
        private readonly int[] _directions;
        private readonly BlockMovementType[] _movementTypes;
        private readonly BlockTags _blockedBy;

        public Swap(int[] priorities, int[] directions, BlockMovementType[] movementTypes, BlockTags blockedBy)
        {
            _priorities = priorities;
            _directions = directions;
            _movementTypes = movementTypes;
            _blockedBy = blockedBy;
        }

        public unsafe bool Execute(Random rng, ChunkNeighborhood chunkNeighborhood, int block, int x, int y,
            int* directionX, int* directionY, int* distances, int* bitCount)
        {
            var availableTargets = stackalloc int[4];
            var targetBlocks = stackalloc int[4];

            // Traverse priority array
            const int maximumDirections = 2;
            for (var i = 0; i < _priorities.Length; i += maximumDirections)
            {
                int loopRange;
                int loopStart;
                if (_priorities[i] == _priorities[i + 1])
                {
                    loopRange = 1;
                    loopStart = 0;
                }
                else
                {
                    loopRange = 2;
                    loopStart = rng.Next(0, loopRange);
                }

                for (var k = 0; k < loopRange; k++)
                {
                    var directionIdx = _priorities[i + (loopStart + k) % loopRange];

                    // we need to check our possible movements in this direction
                    if (!FillAvailableTargets(chunkNeighborhood, x, y, block, directionIdx, directionX,
                        directionY, availableTargets, targetBlocks))
                        continue; // we found no targets, check other direction

                    // we found at least 1 target, proceed to swap
                    var distance = 0;
                    switch (_movementTypes[directionIdx])
                    {
                        case BlockMovementType.Closest:
                            for (var j = 0; j < _directions[directionIdx]; ++j)
                            {
                                if (availableTargets[j] != 1)
                                    continue;
                                distance = j + 1;
                                break;
                            }

                            break;
                        case BlockMovementType.Farthest:
                            for (var j = _directions[directionIdx] - 1; j >= 0; --j)
                            {
                                if (availableTargets[j] != 1)
                                    continue;
                                distance = j + 1;
                                break;
                            }

                            break;
                        case BlockMovementType.Randomized:
                            var index = availableTargets[0]
                                        | (availableTargets[1] << 1)
                                        | (availableTargets[2] << 2)
                                        | (availableTargets[3] << 3);
                            distance = distances[index * 4 + rng.Next(0, bitCount[index])];
                            break;
                    }

                    return chunkNeighborhood.MoveBlock(x, y,
                        distance * directionX[directionIdx],
                        distance * directionY[directionIdx],
                        block, targetBlocks[distance - 1]);
                }
            }

            return false;
        }

        private unsafe bool FillAvailableTargets(ChunkNeighborhood chunkNeighborhood, int x, int y, int block, int directionIdx,
            int* directionX, int* directionY, int* availableTargets, int* targetBlocks)
        {
            var targetsFound = false;
            for (var j = 0; j < _directions[directionIdx]; ++j)
            {
                availableTargets[j] = 0; // default value should be set to 0, otherwise it's garbage value
                targetBlocks[j] = chunkNeighborhood.GetBlockType(x + (j + 1) * directionX[directionIdx], y + (j + 1) * directionY[directionIdx]);
                if (_blockedBy == BlockConstants.BlockDescriptors[targetBlocks[j]].Tag)
                    return targetsFound;

                if (!(BlockConstants.BlockDescriptors[targetBlocks[j]].DensityPriority < BlockConstants.BlockDescriptors[block].DensityPriority))
                    continue;

                availableTargets[j] = 1;
                targetsFound = true;
            }

            return targetsFound;
        }
    }
}
