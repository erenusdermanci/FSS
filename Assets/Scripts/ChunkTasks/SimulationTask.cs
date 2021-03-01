using System;
using DataComponents;
using System.Threading;
using Unity.Collections;
using static Constants;

namespace ChunkTasks
{
    public class SimulationTask : IChunkTask
    {
        public int[] BlockCounts;

        private readonly Chunk[] _chunks;
        private readonly NativeArray<Unity.Mathematics.Random> _randomArray;
        private Unity.Mathematics.Random _rng;

        public SimulationTask(Chunk[] chunks, NativeArray<Unity.Mathematics.Random> randomArray)
        {
            _chunks = chunks;
            _randomArray = randomArray;
            BlockCounts = new int[Enum.GetNames(typeof(Blocks)).Length];
        }

        public void Execute()
        {
            _rng = _randomArray[Thread.CurrentThread.ManagedThreadId];

            for (var y = 0; y < Chunk.Size; ++y)
            {
                for (var x = 0; x < Chunk.Size; ++x)
                {
                    var block = GetBlock(x, y, current: true);
                    if (block == -1 || block == (int) Blocks.Border)
                        continue;

                    switch (block)
                    {
                        case (int)Blocks.Oil:
                            SimulateWater(block, x, y);
                            break;
                        case (int)Blocks.Water:
                            SimulateWater(block, x, y);
                            break;
                        case (int)Blocks.Sand:
                            SimulateSand(block, x, y);
                            break;
                        case (int)Blocks.Air:
                        case (int)Blocks.Stone:
                        case (int)Blocks.Metal:
                            break;
                        default:
                            throw new System.NotImplementedException();
                    }

                    BlockCounts[_chunks[0].BlockTypes[y * Chunk.Size + x]] += 1;
                }
            }
        }

        #region Block logic

        private void SimulateOil(int block)
        {

        }

        private unsafe void SimulateWater(int block, int x, int y)
        {
            // DOWN IS PRIORITY!
            var targetBlocks = stackalloc int[4];
            targetBlocks[0] = GetBlock(x, y - 1);
            targetBlocks[1] = GetBlock(x, y - 2);
            targetBlocks[2] = GetBlock(x, y - 3);
            targetBlocks[3] = GetBlock(x, y - 4);

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
            int index = 0;
            int range = firstTargetAvailable + secondTargetAvailable + thirdTargetAvailable + fourthTargetAvailable;
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
                targetBlocks[0] = GetBlock(x - 1, y - 1);
                targetBlocks[1] = GetBlock(x + 1, y - 1);

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
                    targetBlocks[0] = GetBlock(x - 1, y);
                    targetBlocks[1] = GetBlock(x - 2, y);
                    // Right
                    targetBlocks[2] = GetBlock(x + 1, y);
                    targetBlocks[3] = GetBlock(x + 2, y);

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
                        return; // could not do anything
                    if (total == 4)
                        index = _rng.NextInt(0, 5); // all available
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

                    PutBlock(x + targetDirection[index], y, block);
                    PutBlock(x, y, targetBlocks[index]);
                    return;
                }

                PutBlock(x - (index == 0 ? 1 : -1), y - 1, block);
                PutBlock(x, y, targetBlocks[index]);
                return; // Otherwise we will duplicate the block
            }
            
            PutBlock(x, y - (index + 1), block);
            PutBlock(x, y, targetBlocks[index]);
        }

        private unsafe void SimulateSand(int block, int x, int y)
        {
            // DOWN IS PRIORITY!
            var targetBlocks = stackalloc int[2];
            targetBlocks[0] = GetBlock(x, y - 1);
            targetBlocks[1] = GetBlock(x, y - 2);

            var firstTargetAvailable = false;
            var secondTargetAvailable = false;

            if (targetBlocks[0] < block) // available slot
            {
                firstTargetAvailable = true;
            }
            if (targetBlocks[1] < block && targetBlocks[0] <= block)
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
                targetBlocks[0] = GetBlock(x - 1, y - 1);
                targetBlocks[1] = GetBlock(x + 1, y - 1);

                if (targetBlocks[0] < block) // available slot
                {
                    firstTargetAvailable = true;
                }

                if (targetBlocks[1] < block)
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
                    return;
                }

                PutBlock(x - (index == 0 ? 1 : -1), y - 1, block);
                PutBlock(x, y, targetBlocks[index]);
                return; // Otherwise we will duplicate the block
            }

            PutBlock(x, y - (index + 1), block);
            PutBlock(x, y, targetBlocks[index]);
        }

        #endregion

        #region Common methods

        private int GetBlock(int x, int y, bool current = false)
        {
            var chunkIndex = 0;
            UpdateCoordinates(ref x, ref y, ref chunkIndex);

            if (_chunks[chunkIndex] == null)
                return (int)Blocks.Border;

            var blockIndex = y * Chunk.Size + x;
            if (current)
            {
                var blockCooldown = _chunks[chunkIndex].BlockUpdateCooldowns[blockIndex];
                if (blockCooldown > 0)
                {
                    _chunks[chunkIndex].BlockUpdateCooldowns[blockIndex]--;
                    return -1;
                }
            }

            return _chunks[chunkIndex].BlockTypes[blockIndex];
        }

        private void PutBlock(int x, int y, int type)
        {
            var chunkIndex = 0;
            UpdateCoordinates(ref x, ref y, ref chunkIndex);

            if (_chunks[chunkIndex] == null)
                return;

            var i = y * Chunk.Size + x;

            _chunks[chunkIndex].BlockColors[i * 4] = BlockColors[type].r;
            _chunks[chunkIndex].BlockColors[i * 4 + 1] = BlockColors[type].g;
            _chunks[chunkIndex].BlockColors[i * 4 + 2] = BlockColors[type].b;
            _chunks[chunkIndex].BlockColors[i * 4 + 3] = BlockColors[type].a;
            _chunks[chunkIndex].BlockTypes[y * Chunk.Size + x] = type;
            if (chunkIndex != 0)
                _chunks[chunkIndex].BlockUpdateCooldowns[y * Chunk.Size + x] = 1;
        }

        private static void UpdateCoordinates(ref int x, ref int y, ref int chunkIndex)
        {
            chunkIndex = 0;
            if (x < 0 && y < 0)
            {
                // Down Left
                chunkIndex = 1;
                x = Chunk.Size + x;
                y = Chunk.Size + y;
            }
            else if (x >= Chunk.Size && y < 0)
            {
                // Down Right
                chunkIndex = 3;
                x -= Chunk.Size;
                y = Chunk.Size + y;
            }
            else if (y < 0)
            {
                // Down
                chunkIndex = 2;
                y = Chunk.Size + y;
            }
            else if (x < 0 && y >= Chunk.Size)
            {
                // Up Left
                chunkIndex = 6;
                y -= Chunk.Size;
                x = Chunk.Size + x;
            }
            else if (x >= Chunk.Size && y >= Chunk.Size)
            {
                // Up Right
                chunkIndex = 8;
                y -= Chunk.Size;
                x -= Chunk.Size;
            }
            else if (y >= Chunk.Size)
            {
                // Up
                chunkIndex = 7;
                y -= Chunk.Size;
            }
            else if (x < 0)
            {
                // Left
                chunkIndex = 4;
                x = Chunk.Size + x;
            }
            else if (x >= Chunk.Size)
            {
                // Right
                chunkIndex = 5;
                x -= Chunk.Size;
            }
        }

        #endregion
    }
}