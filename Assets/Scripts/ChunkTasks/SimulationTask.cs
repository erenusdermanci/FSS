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

        public NativeArray<Unity.Mathematics.Random> RandomArray;
        private Unity.Mathematics.Random _rng;

        public SimulationTask(Chunk chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            _rng = RandomArray[Thread.CurrentThread.ManagedThreadId];

            for (var i = 0; i < BlockCounts.Length; ++i)
                BlockCounts[i] = 0;

            var blockMoveInfo = new ChunkNeighborhood.BlockMoveInfo();

            var moved = false;
            for (var y = 0; y < Chunk.Size; ++y)
            {
                for (var x = 0; x < Chunk.Size; ++x)
                {
                    blockMoveInfo.Chunk = -1;
                    blockMoveInfo.X = -1;
                    blockMoveInfo.Y = -1;
                    
                    var block = Chunks.GetBlock(x, y, current: true);
                    if (block == CooldownBlockValue || block == (int)Blocks.Border)
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
                    BlockCounts[Chunks[0].BlockTypes[y * Chunk.Size + x]] += 1;
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
            var targetBlocks = stackalloc int[4];
            targetBlocks[0] = Chunks.GetBlock(x, y - 1);
            targetBlocks[1] = Chunks.GetBlock(x, y - 2);
            targetBlocks[2] = Chunks.GetBlock(x, y - 3);
            targetBlocks[3] = Chunks.GetBlock(x, y - 4);

            var firstTargetAvailable = 0;
            var secondTargetAvailable = 0;
            var thirdTargetAvailable = 0;
            var fourthTargetAvailable = 0;

            if (targetBlocks[0] < block)
            {
                firstTargetAvailable = 1;
            }

            if (targetBlocks[0] <= block)
            {
                if (targetBlocks[1] < block)
                {
                    secondTargetAvailable = 1;
                }

                if (targetBlocks[1] <= block)
                {
                    if (targetBlocks[2] < block)
                    {
                        thirdTargetAvailable = 1;
                    }

                    if (targetBlocks[2] <= block)
                    {
                        if (targetBlocks[3] < block)
                        {
                            fourthTargetAvailable = 1;
                        }
                    }
                }
            }

            // start determining the index to put block @
            var index = 0;
            var range = firstTargetAvailable + secondTargetAvailable + thirdTargetAvailable + fourthTargetAvailable;
            if (range > 1)
            {
                // random
                index = _rng.NextInt(0, range);
            }
            else if (firstTargetAvailable == 1)
            {
                index = 0;
            }
            else if (secondTargetAvailable == 1)
            {
                index = 1;
            }
            else if (thirdTargetAvailable == 1)
            {
                index = 2;
            }
            else if (fourthTargetAvailable == 1)
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
                    firstTargetAvailable = 1;
                }

                if (targetBlocks[1] < block)
                {
                    secondTargetAvailable = 1;
                }

                range = firstTargetAvailable + secondTargetAvailable;
                // start determining the index to put block
                if (range > 1)
                {
                    // random
                    index = _rng.NextInt(0, range);
                }
                else if (firstTargetAvailable == 1)
                {
                    index = 0;
                }
                else if (secondTargetAvailable == 1)
                {
                    index = 1;
                }
                else // COULD NOT MOVE LEFT DOWN RIGHT DOWN, we move HORIZONTALLY
                {
                    // Left
                    targetBlocks[0] = Chunks.GetBlock(x - 1, y);
                    targetBlocks[1] = Chunks.GetBlock(x - 2, y);
                    // Right
                    targetBlocks[2] = Chunks.GetBlock(x + 1, y);
                    targetBlocks[3] = Chunks.GetBlock(x + 2, y);

                    var targetDirection = stackalloc int[4] { -1, -2, 1, 2 };

                    if (targetBlocks[0] < block)
                    {
                        firstTargetAvailable = 1;
                    }

                    if (targetBlocks[0] <= block)
                    {
                        if (targetBlocks[1] < block)
                        {
                            secondTargetAvailable = 1;
                        }
                    }

                    if (targetBlocks[2] < block)
                    {
                        thirdTargetAvailable = 1;
                    }

                    if (targetBlocks[2] <= block)
                    {
                        if (targetBlocks[3] < block)
                        {
                            fourthTargetAvailable = 1;
                        }
                    }
                    var total = firstTargetAvailable + secondTargetAvailable + thirdTargetAvailable +
                                fourthTargetAvailable;
                    if (total == 0)
                        return false; // could not do anything
                    if (total == 4)
                        index = _rng.NextInt(0, 4); // all available
                    else
                    {
                        var min = stackalloc int[3] {1, 0, 0};
                        var max = stackalloc int[3] {2, 1, 2};
                        var idx = (((firstTargetAvailable | secondTargetAvailable) << 1) |
                                   (thirdTargetAvailable | fourthTargetAvailable)) - 1;

                        // 0 -> right
                        // 1 -> left
                        // 2 -> both
                        // 0001 -> 01 (1) -> 1
                        // 0010 -> 01 -> 1
                        // 0011 -> 01 -> 1
                        // 0100 -> 10 (2) -> 0
                        // 0101 -> 11 (3) -> 2
                        // 0110 -> 11 -> 2
                        // 0111 -> 11 -> 2
                        // 1000 -> 10 -> 0
                        // 1001 -> 11 -> 2
                        // 1010 -> 11 -> 2
                        // 1011 -> 11 -> 2
                        // 1100 -> 10 -> 0
                        // 1101 -> 11 -> 2
                        // 1110 -> 11 -> 2
                        var rngSide = _rng.NextInt(min[idx], max[idx]);
                        if (rngSide == 0)
                        {
                            // Left
                            // start determining the index to put block
                            if (firstTargetAvailable + secondTargetAvailable == 2)
                            {
                                // random
                                index = _rng.NextInt(0, 2);
                            }
                            else if (firstTargetAvailable == 1)
                            {
                                index = 0;
                            }
                            else if (secondTargetAvailable == 1)
                            {
                                index = 1;
                            }
                        }
                        else
                        {
                            // Right
                            // start determining the index to put block
                            if (thirdTargetAvailable + fourthTargetAvailable == 2)
                            {
                                // random
                                index = _rng.NextInt(2, 4);
                            }
                            else if (thirdTargetAvailable == 1)
                            {
                                index = 2;
                            }
                            else if (fourthTargetAvailable == 1)
                            {
                                index = 3;
                            }
                        }
                    }

                    
                    /*
                    Chunks.PutBlock(x + targetDirection[index], y, block, true, true);
                    Chunks.PutBlock(x, y, targetBlocks[index]);
                    return;
                }

                Chunks.PutBlock(x - (index == 0 ? 1 : -1), y - 1, block, true, true);
                Chunks.PutBlock(x, y, targetBlocks[index]);
                return; // Otherwise we will duplicate the block
            }

            Chunks.PutBlock(x, y - (index + 1), block, true, true);
            Chunks.PutBlock(x, y, targetBlocks[index]);
            */
                    
                    
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