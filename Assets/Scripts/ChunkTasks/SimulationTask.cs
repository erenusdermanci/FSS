using System;
using DataComponents;
using System.Threading;
using Unity.Collections;
using Utils;
using static Constants;

namespace ChunkTasks
{
    public class SimulationTask : ChunkTask
    {
        internal ChunkNeighborhood Chunks;

        public ThreadLocal<Unity.Mathematics.Random> Random;

        private Unity.Mathematics.Random _rng;

        public SimulationTask(Chunk chunk) : base(chunk)
        {

        }

        protected override unsafe void Execute()
        {
            _rng = Random.Value;

            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            var blockMoveInfo = new ChunkNeighborhood.BlockMoveInfo();
            var xStart = stackalloc int[2] { 0, Chunk.Size - 1 };
            var xCmp = stackalloc int[2] { Chunk.Size, -1 };
            var xInc = stackalloc int[2] { +1, -1 };

            var moved = false;
            for (var y = 0; y < Chunk.Size; ++y)
            {
                var xDir = _rng.NextInt(0, 2);
                for (var x = xStart[xDir]; x != xCmp[xDir]; x += xInc[xDir])
                {
                    blockMoveInfo.Chunk = -1;
                    blockMoveInfo.X = -1;
                    blockMoveInfo.Y = -1;

                    if (Chunks[0].BlockUpdatedFlags[y * Chunk.Size + x] == 1)
                        continue;

                    var block = Chunks.GetBlock(x, y, current: true);

                    if (block == (int)Blocks.Border)
                        continue;

                    switch (block)
                    {
                        case (int)Blocks.Oil:
                            moved |= SimulateWater(block, x, y, ref blockMoveInfo);
                            break;
                        case (int)Blocks.Water:
                            moved |= SimulateWater(block, x, y, ref blockMoveInfo);
                            break;
                        case (int)Blocks.Sand:
                            moved |= SimulateSand(block, x, y, ref blockMoveInfo);
                            break;
                        case (int)Blocks.Air:
                        case (int)Blocks.Stone:
                        case (int)Blocks.Metal:
                        case (int)Blocks.Dirt:
                        case (int)Blocks.Cloud:
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (blockMoveInfo.Chunk > 0)
                    {
                        Chunks[blockMoveInfo.Chunk].Dirty = true;
                    }
                }
            }

            Chunk.Dirty = moved;

            for (var y = 0; y < Chunk.Size; ++y)
            {
                for (var x = 0; x < Chunk.Size; ++x)
                {
                    var i = y * Chunk.Size + x;
                    Chunk.BlockCounts[Chunks[0].blockTypes[i]] += 1;
                    Chunk.BlockUpdatedFlags[i] = 0;
                }
            }
        }

        #region Block logic

        private bool SimulateOil(int block, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo)
        {
            return false;
        }

        private unsafe bool SimulateWater(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo)
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
                index = _rng.NextInt(0, range);
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
                    index = _rng.NextInt(0, range);
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
                        index = _rng.NextInt(0, 8);
                    }
                    else
                    {
                        // there is 1 to 7 blocks available
                        
                        
                        var rngSideMinIndex = stackalloc int[3] {1, 0, 0};
                        var rngSideMaxIndex = stackalloc int[3] {2, 1, 2};
                        // we collapse the left side and the right side to obtain a 2 bit value ranging into [1,2,3]
                        // and subtract 1 to index into rngSide
                        var blockIndex =
                            (((targetAvailable[0] | targetAvailable[1] | targetAvailable[2] | targetAvailable[3]) << 1)
                             | targetAvailable[4] | targetAvailable[5] | targetAvailable[6] | targetAvailable[7]) - 1;

                        var indexes = stackalloc int[64]
                        {
                            0,0,0,0, // should not happen
                            0,0,0,0,
                            1,0,0,0,
                            0,1,0,0,
                            2,0,0,0,
                            0,2,0,0,
                            1,2,0,0,
                            0,1,2,0,
                            3,0,0,0,
                            0,3,0,0,
                            1,3,0,0,
                            0,1,3,0,
                            2,3,0,0,
                            0,2,3,0,
                            1,2,3,0,
                            0,1,2,3
                        };
                        var bitCount = stackalloc int[16] { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };
                        
                        var rngSide = _rng.NextInt(rngSideMinIndex[blockIndex], rngSideMaxIndex[blockIndex]);
                        if (rngSide == 0)
                        {

                            var i = targetAvailable[0]
                                    | (targetAvailable[1] << 1)
                                    | (targetAvailable[2] << 2)
                                    | (targetAvailable[3] << 3);
                            index = indexes[i * 4 + _rng.NextInt(0, bitCount[i])];
                        }
                        else
                        {
                            var i = targetAvailable[4]
                                    | (targetAvailable[5] << 1)
                                    | (targetAvailable[6] << 2)
                                    | (targetAvailable[7] << 3);
                            index = indexes[i * 4 + _rng.NextInt(0, bitCount[i])] + 4;
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
                index = _rng.NextInt(0, 2);
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
                    index = _rng.NextInt(0, 2);
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