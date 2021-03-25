using System;
using Chunks;

namespace Blocks.Behaviors
{
    public class Swapper : IBehavior
    {
        private readonly int[] _priorities;
        private readonly int[] _directions;
        private readonly BlockMovementType[] _movementTypes;
        private readonly BlockTags _blockedBy;

        public Swapper(int[] priorities, int[] directions, BlockMovementType[] movementTypes, BlockTags blockedBy)
        {
            _priorities = priorities;
            _directions = directions;
            _movementTypes = movementTypes;
            _blockedBy = blockedBy;
        }

        public unsafe bool Execute(Random rng, ChunkServerNeighborhood chunkNeighborhood, int block, int x, int y,
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

        private unsafe bool FillAvailableTargets(ChunkServerNeighborhood chunkNeighborhood, int x, int y, int block, int directionIdx,
            int* directionX, int* directionY, int* availableTargets, int* targetBlocks)
        {
            var targetsFound = false;
            for (var j = 0; j < _directions[directionIdx]; ++j)
            {
                availableTargets[j] = 0; // default value should be set to 0, otherwise it's garbage value
                var targetX = x + (j + 1) * directionX[directionIdx];
                var targetY = y + (j + 1) * directionY[directionIdx];
                targetBlocks[j] = chunkNeighborhood.GetBlockType(targetX, targetY);

                // collision, we cannot continue further in this direction
                if (_blockedBy == BlockConstants.BlockDescriptors[targetBlocks[j]].Tag)
                    return targetsFound;

                // density logic check
                // when swapping vertically, we need to make sure the blocks respect density rules
                var densityDiff = BlockConstants.BlockDescriptors[targetBlocks[j]].DensityPriority -
                                  BlockConstants.BlockDescriptors[block].DensityPriority;

                // same density value, should not swap with same block
                if (densityDiff == 0f)
                    continue;

                // block horizontal movement if densities need to be respected: they have priority
                var upBlock = chunkNeighborhood.GetBlockType(x, y + 1);
                if (directionY[directionIdx] == 0 && targetBlocks[j] != BlockConstants.Air)
                {
                    if (densityDiff < 0.0f)
                    {
                        if (targetBlocks[j] != upBlock)
                            continue;
                    }
                    else
                    {
                        if (block != upBlock)
                            continue;
                    }
                }

                // if moving vertically and density is different, lets check if the densities are already properly ordered
                if (directionY[directionIdx] != 0 && directionY[directionIdx] * densityDiff <= 0.0f)
                    continue;

                // target was already updated during this simulation frame, not a valid target
                if (chunkNeighborhood.GetBlockUpdatedFlag(targetX, targetY) == ChunkManager.UpdatedFlag)
                    continue;

                // all criteria met, target is available
                availableTargets[j] = 1;
                targetsFound = true;
            }

            return targetsFound;
        }
    }
}
