using System;
using System.Runtime.CompilerServices;
using System.Threading;
using BlockBehavior;
using DataComponents;
using Utils;
using static BlockConstants;

namespace ChunkTasks
{
    public class SimulationTask : ChunkTask
    {
        internal ChunkNeighborhood Chunks;

        public ThreadLocal<Random> Random;

        private Random _rng;

        private readonly int[] _indexingOrder;

        public SimulationTask(Chunk chunk) : base(chunk)
        {
            var size = Chunk.Size * Chunk.Size;
            _indexingOrder = new int[size];
            
            for (var i = 0; i < size; ++i)
            {
                _indexingOrder[i] = i;
            }

            var rng = new Random();

            while (size > 1)
            {
                var i = rng.Next(size--);
                var tmp = _indexingOrder[size];
                _indexingOrder[size] = _indexingOrder[i];
                _indexingOrder[i] = tmp;
            }
        }

        protected override unsafe void Execute()
        {
            #region stackalloc declared at the top for performance
            var distances = stackalloc int[64]
            {
                0,0,0,0, // should not happen
                1,1,1,1,
                2,1,1,1,
                1,2,1,1,
                3,1,1,1,
                1,3,1,1,
                2,3,1,1,
                1,2,3,1,
                4,1,1,1,
                1,4,1,1,
                2,4,1,1,
                1,2,4,1,
                3,4,1,1,
                1,3,4,1,
                2,3,4,1,
                1,2,3,4
            };
            var bitCount = stackalloc int[16] { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
            var directionX = stackalloc int[] { 0, -1, 1, -1, 1, 0, -1, 1 };
            var directionY = stackalloc int[] { -1, -1, -1, 0, 0, 1, 1, 1 };
            var availableTargets = stackalloc int[4];
            var targetBlocks = stackalloc int[4];
            #endregion
            
            _rng = Random.Value;

            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            var blockMoveInfo = new ChunkNeighborhood.BlockMoveInfo();

            var moved = false;
            const int totalSize = Chunk.Size * Chunk.Size;
            for (var i = 0; i < totalSize; ++i)
            {
                var x = _indexingOrder[i] / Chunk.Size;
                var y = _indexingOrder[i] % Chunk.Size;
                blockMoveInfo.Chunk = -1;
                blockMoveInfo.X = -1;
                blockMoveInfo.Y = -1;

                if (Chunks[0].BlockUpdatedFlags[y * Chunk.Size + x] == 1)
                    continue;

                var block = Chunks.GetBlock(x, y, true);

                if (block == (int)Blocks.Border)
                    continue;

                moved |= SimulateBlock(block, x, y, ref blockMoveInfo, distances, bitCount, directionX, directionY, availableTargets, targetBlocks);

                if (blockMoveInfo.Chunk > 0)
                {
                    Chunks[blockMoveInfo.Chunk].Dirty = true;
                }
            }

            Chunk.Dirty = moved;

            for (var y = 0; y < Chunk.Size; ++y)
            {
                for (var x = 0; x < Chunk.Size; ++x)
                {
                    var i = y * Chunk.Size + x;
                    Chunk.BlockCounts[Chunks[0].Data.types[i]] += 1;
                    Chunk.BlockUpdatedFlags[i] = 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool SimulateBlock(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo,
                                        int* distances, int* bitCount, int* directionX, int* directionY, int* availableTargets, int* targetBlocks)
        {
            var blockLogic = BlockLogic.BlockDescriptors[block];

            foreach (var behavior in blockLogic.Behaviors)
            {
                switch (behavior.Id)
                {
                    case 0: // Swap
                        var swap = (Swap) behavior;

                        // Traverse priority array
                        const int maximumDirections = 2;
                        for (var i = 0; i < swap.Priorities.Length; i += maximumDirections)
                        {
                            int loopRange;
                            int loopStart;
                            if (swap.Priorities[i] == swap.Priorities[i + 1])
                            {
                                loopRange = 1;
                                loopStart = 0;
                            }
                            else
                            {
                                loopRange = 2;
                                loopStart = _rng.Next(0, loopRange);
                            }

                            for (var k = 0; k < loopRange; k++)
                            {
                                var directionIdx = swap.Priorities[i + (loopStart + k) % loopRange];

                                // we need to check our possible movements in this direction
                                if (!FillAvailableTargets(swap, x, y, block, directionIdx, directionX,
                                    directionY, availableTargets, targetBlocks))
                                    continue; // we found no targets, check other direction

                                // we found at least 1 target, proceed to swap
                                var distance = 0;
                                switch (swap.MovementTypes[directionIdx])
                                {
                                    case MovementType.Closest:
                                        for (var j = 0; j < 4; ++j)
                                        {
                                            if (availableTargets[j] != 1)
                                                continue;
                                            distance = j + 1;
                                            break;
                                        }
                                        break;
                                    case MovementType.Farthest:
                                        for (var j = 3; j >= 0; --j)
                                        {
                                            if (availableTargets[j] != 1)
                                                continue;
                                            distance = j + 1;
                                            break;
                                        }
                                        break;
                                    case MovementType.Randomized:
                                        var index = availableTargets[0]
                                                    | (availableTargets[1] << 1)
                                                    | (availableTargets[2] << 2)
                                                    | (availableTargets[3] << 3);
                                        distance = distances[index * 4 + _rng.Next(0, bitCount[index])];
                                        break;
                                }
                                var dx = distance * directionX[directionIdx];
                                var dy = distance * directionY[directionIdx];
                                return Chunks.MoveBlock(x, y, dx, dy, block, targetBlocks[distance - 1], ref blockMoveInfo);
                            }
                        }
                        break;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool FillAvailableTargets(Swap swap, int x, int y, int block, int directionIdx,
                                                int* directionX, int* directionY, int* availableTargets, int* targetBlocks)
        {
            var stop = false;
            var targetsFound = false;
            for (var j = 0; j < swap.Directions[directionIdx] && !stop; ++j)
            {
                var dx = (j + 1) * directionX[directionIdx];
                var dy = (j + 1) * directionY[directionIdx];

                targetBlocks[j] = Chunks.GetBlock(x + dx, y + dy);

                switch (swap.MovementTypes[directionIdx])
                {
                    case MovementType.Closest:
                        if (IsTargetAvailable(swap, block, targetBlocks[j]))
                        {
                            availableTargets[j] = 1;
                            return true;
                        }
                        break;
                    case MovementType.Farthest:
                        // TODO
                        break;
                    case MovementType.Randomized:
                        if (swap.BlockedBy.Equals(BlockLogic.BlockDescriptors[targetBlocks[j]].PhysicalTag))
                        {
                            availableTargets[j] = 0;
                            stop = true;
                        }
                        else
                        {
                            availableTargets[j] = BlockLogic.BlockDescriptors[targetBlocks[j]].Density < BlockLogic.BlockDescriptors[block].Density ? 1 : 0;
                            if (availableTargets[j] == 1)
                                targetsFound = true;
                        }
                        break;
                }
            }

            return targetsFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTargetAvailable(Swap swap, int block, int targetBlock)
        {
            if (swap.BlockedBy.Equals(BlockLogic.BlockDescriptors[targetBlock].PhysicalTag))
                return true;

            return BlockLogic.BlockDescriptors[targetBlock].Density < BlockLogic.BlockDescriptors[block].Density;
        }

        #region Block logic

        private unsafe bool SimulateWater(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo,
                                          int* indexes, int* bitCount)
        {
            // DOWN IS PRIORITY!
            var targetBlocks = stackalloc int[8];
            targetBlocks[0] = Chunks.GetBlock(x, y - 1);
            targetBlocks[1] = Chunks.GetBlock(x, y - 2);
            targetBlocks[2] = Chunks.GetBlock(x, y - 3);
            targetBlocks[3] = Chunks.GetBlock(x, y - 4);

            var targetAvailable = stackalloc int[8];

            if (targetBlocks[0] < block)
            {
                targetAvailable[0] = 1;
            }

            if (targetBlocks[0] <= block)
            {
                if (targetBlocks[1] < block)
                {
                    targetAvailable[1] = 1;
                }

                if (targetBlocks[1] <= block)
                {
                    if (targetBlocks[2] < block)
                    {
                        targetAvailable[2] = 1;
                    }

                    if (targetBlocks[2] <= block)
                    {
                        if (targetBlocks[3] < block)
                        {
                            targetAvailable[3] = 1;
                        }
                    }
                }
            }

            // start determining the index to put block @
            int index;
            var range = targetAvailable[0] + targetAvailable[1] + targetAvailable[2] + targetAvailable[3];
            if (range > 1)
            {
                // random
                index = _rng.Next(0, range);
            }
            else if (targetAvailable[0] == 1)
            {
                index = 0;
            }
            else if (targetAvailable[1] == 1)
            {
                index = 1;
            }
            else if (targetAvailable[2] == 1)
            {
                index = 2;
            }
            else if (targetAvailable[3] == 1)
            {
                index = 3;
            }
            else // none available directly down
            {
                // WE COULDNT MOVE DIRECTLY DOWN, NOW WE CHECK AROUND X AS WELL
                targetBlocks[0] = Chunks.GetBlock(x - 1, y - 1);
                targetBlocks[1] = Chunks.GetBlock(x + 1, y - 1);

                if (targetBlocks[0] < block) // available slot
                {
                    targetAvailable[0] = 1;
                }

                if (targetBlocks[1] < block)
                {
                    targetAvailable[1] = 1;
                }

                range = targetAvailable[0] + targetAvailable[1];
                // start determining the index to put block
                if (range > 1)
                {
                    // random
                    index = _rng.Next(0, range);
                }
                else if (targetAvailable[0] == 1)
                {
                    index = 0;
                }
                else if (targetAvailable[1] == 1)
                {
                    index = 1;
                }
                else // COULD NOT MOVE LEFT DOWN RIGHT DOWN, we move HORIZONTALLY
                {
                    var targetDirection = stackalloc int[8] { -1, -2, -3, -4, 1, 2, 3, 4 };

                    for (var i = 0; i < 8; ++i)
                        targetBlocks[i] = Chunks.GetBlock(x + targetDirection[i], y);

                    // Check targets to the left
                    targetAvailable[0] = targetBlocks[0] < block ? 1 : 0;
                    if (targetBlocks[0] <= block)
                    {
                        targetAvailable[1] = targetBlocks[1] < block ? 1 : 0;
                        if (targetBlocks[1] <= block)
                        {
                            targetAvailable[2] = targetBlocks[2] < block ? 1 : 0;
                            if (targetBlocks[2] <= block)
                            {
                                targetAvailable[3] = targetBlocks[3] < block ? 1 : 0;
                            }
                        }
                    }
                    
                    // Check targets to the right
                    targetAvailable[4] = targetBlocks[4] < block ? 1 : 0;
                    if (targetBlocks[4] <= block)
                    {
                        targetAvailable[5] = targetBlocks[5] < block ? 1 : 0;
                        if (targetBlocks[5] <= block)
                        {
                            targetAvailable[6] = targetBlocks[6] < block ? 1 : 0;
                            if (targetBlocks[6] <= block)
                            {
                                targetAvailable[7] = targetBlocks[7] < block ? 1 : 0;
                            }
                        }
                    }

                    var total = 0;
                    for (var i = 0; i < 8; ++i)
                        total += targetAvailable[i];
                    if (total == 0)
                    {
                        // could not do anything
                        return false;
                    }
                    if (total == 8)
                    {
                        // all blocks are available
                        index = _rng.Next(0, 8);
                    }
                    else // what could go wrong...
                    {
                        // there is 1 to 7 blocks available
                        
                        
                        var rngSideMinIndex = stackalloc int[3] {1, 0, 0};
                        var rngSideMaxIndex = stackalloc int[3] {2, 1, 2};
                        // we collapse the left side and the right side to obtain a 2 bit value ranging into [1,2,3]
                        // and subtract 1 to index into rngSide
                        var blockIndex =
                            (((targetAvailable[0] | targetAvailable[1] | targetAvailable[2] | targetAvailable[3]) << 1)
                             | targetAvailable[4] | targetAvailable[5] | targetAvailable[6] | targetAvailable[7]) - 1;
                        
                        var rngSide = _rng.Next(rngSideMinIndex[blockIndex], rngSideMaxIndex[blockIndex]);
                        if (rngSide == 0)
                        {
                            var i = targetAvailable[0]
                                    | (targetAvailable[1] << 1)
                                    | (targetAvailable[2] << 2)
                                    | (targetAvailable[3] << 3);
                            index = indexes[i * 4 + _rng.Next(0, bitCount[i])];
                        }
                        else
                        {
                            var i = targetAvailable[4]
                                    | (targetAvailable[5] << 1)
                                    | (targetAvailable[6] << 2)
                                    | (targetAvailable[7] << 3);
                            index = indexes[i * 4 + _rng.Next(0, bitCount[i])] + 4;
                        }
                    }

                    return Chunks.MoveBlock(x, y, targetDirection[index], 0, block, targetBlocks[index], ref blockMoveInfo);
                }

                return Chunks.MoveBlock(x, y, -(index == 0 ? 1 : -1), -1, block, targetBlocks[index], ref blockMoveInfo);
            }

            return Chunks.MoveBlock(x, y, 0, -(index + 1), block, targetBlocks[index], ref blockMoveInfo);
        }

        private unsafe bool SimulateSand(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo)
        {
            // DOWN IS PRIORITY!
            var targetBlocks = stackalloc int[2];
            targetBlocks[0] = Chunks.GetBlock(x, y - 1);
            targetBlocks[1] = Chunks.GetBlock(x, y - 2);

            var firstTargetAvailable = false;
            var secondTargetAvailable = false;

            if (targetBlocks[0] == (int) Blocks.Air) // available slot
            {
                firstTargetAvailable = true;
            }
            if (targetBlocks[1] == (int)Blocks.Air &&
                (targetBlocks[0] == (int)Blocks.Air || targetBlocks[0] == block))
            {
                secondTargetAvailable = true;
            }

            // start determining the index to put block @
            int index;
            if (firstTargetAvailable && secondTargetAvailable)
            {
                // random
                index = _rng.Next(0, 2);
            }
            else if (firstTargetAvailable)
            {
                index = 0;
            }
            else if (secondTargetAvailable)
            {
                index = 1;
            }
            else // none available directly down
            {
                // WE COULDNT MOVE DIRECTLY DOWN, NOW WE CHECK AROUND X AS WELL
                targetBlocks[0] = Chunks.GetBlock(x - 1, y - 1);
                targetBlocks[1] = Chunks.GetBlock(x + 1, y - 1);

                if (targetBlocks[0] == (int)Blocks.Air) // available slot
                {
                    firstTargetAvailable = true;
                }

                if (targetBlocks[1] == (int)Blocks.Air)
                {
                    secondTargetAvailable = true;
                }

                // start determining the index to put block
                if (firstTargetAvailable && secondTargetAvailable)
                {
                    // random
                    index = _rng.Next(0, 2);
                }
                else if (firstTargetAvailable)
                {
                    index = 0;
                }
                else if (secondTargetAvailable)
                {
                    index = 1;
                }
                else // none available
                {
                    // COULD NOT MOVE AT ALL!
                    return false;
                }
                
                return Chunks.MoveBlock(x, y, -(index == 0 ? 1 : -1), -1, block, targetBlocks[index], ref blockMoveInfo);
            }

            return Chunks.MoveBlock(x, y, 0, -(index + 1), block, targetBlocks[index], ref blockMoveInfo);
        }

        #endregion
    }
}