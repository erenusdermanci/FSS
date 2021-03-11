using System;
using System.Threading;
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
            var indexes = stackalloc int[64]
            {
                0,0,0,0,
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
            var targetBlocks = stackalloc int[8];
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
                var b = y * Chunk.Size + x;
                if (Chunks[0].BlockUpdatedFlags[b] == 1)
                    continue;

                var block = Chunk.Data.types[b];
                switch (block)
                {
                    case (int)Blocks.Wood:
                    case (int)Blocks.Stone:
                    case (int)Blocks.Metal:
                    case (int)Blocks.Dirt:
                    case (int)Blocks.Cloud:
                        continue;
                }
                
                blockMoveInfo.Chunk = -1;
                blockMoveInfo.X = -1;
                blockMoveInfo.Y = -1;

                switch (block)
                {
                    case (int)Blocks.Oil:
                        moved |= SimulateWater(block, x, y, ref blockMoveInfo, indexes, bitCount, targetBlocks);
                        break;
                    case (int)Blocks.Water:
                        moved |= SimulateWater(block, x, y, ref blockMoveInfo, indexes, bitCount, targetBlocks);
                        break;
                    case (int)Blocks.Sand:
                        moved |= SimulateSand(block, x, y, ref blockMoveInfo);
                        break;
                    case (int)Blocks.Fire:
                        moved |= SimulateFire(block, x, y, ref blockMoveInfo);
                        break;
                }

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

        private unsafe bool SimulateFire(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo)
        {
            var targetX = stackalloc int[8] { -1, 0, 1, -1, 1, -1, 0, 1 };
            var targetY = stackalloc int[8] { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (var i = 0; i < 8; ++i)
            {
                var target = Chunks.GetBlock(x + targetX[i], y + targetY[i]);
                var flammability = BlockFlammabilityChance[target];
                if (flammability <= 0.0f)
                    continue;
                if (flammability >= 1.0f)
                {
                    Chunks.MoveBlock(x, y, targetX[i], targetY[i], block, block, ref blockMoveInfo);
                }
                else if (_rng.NextDouble() < flammability)
                {
                    Chunks.MoveBlock(x, y, targetX[i], targetY[i], block, block, ref blockMoveInfo);
                }
            }
            return false;
        }

        #region Block logic

        private unsafe bool SimulateWater(int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo,
                                          int* indexes, int* bitCount, int* targetBlocks)
        {
            // DOWN IS PRIORITY!
            var targetAvailable = stackalloc int[8];

            targetBlocks[0] = Chunks.GetBlock(x, y - 1);
            if (targetBlocks[0] < block)
                targetAvailable[0] = 1;
            if (targetBlocks[0] <= block)
            {
                targetBlocks[1] = Chunks.GetBlock(x, y - 2);
                if (targetBlocks[1] < block)
                    targetAvailable[1] = 1;
                if (targetBlocks[1] <= block)
                {
                    targetBlocks[2] = Chunks.GetBlock(x, y - 3);
                    if (targetBlocks[2] < block)
                        targetAvailable[2] = 1;
                    if (targetBlocks[2] <= block)
                    {
                        targetBlocks[3] = Chunks.GetBlock(x, y - 4);
                        if (targetBlocks[3] < block)
                            targetAvailable[3] = 1;
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
                    targetAvailable[0] = 1;
                if (targetBlocks[1] < block)
                    targetAvailable[1] = 1;

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

                    targetBlocks[0] = Chunks.GetBlock(x + targetDirection[0], y);
                    // Check targets to the left
                    targetAvailable[0] = targetBlocks[0] < block ? 1 : 0;
                    if (targetBlocks[0] <= block)
                    {
                        targetBlocks[1] = Chunks.GetBlock(x + targetDirection[1], y);
                        targetAvailable[1] = targetBlocks[1] < block ? 1 : 0;
                        if (targetBlocks[1] <= block)
                        {
                            targetBlocks[2] = Chunks.GetBlock(x + targetDirection[2], y);
                            targetAvailable[2] = targetBlocks[2] < block ? 1 : 0;
                            if (targetBlocks[2] <= block)
                            {
                                targetBlocks[3] = Chunks.GetBlock(x + targetDirection[3], y);
                                targetAvailable[3] = targetBlocks[3] < block ? 1 : 0;
                            }
                        }
                    }
                    
                    // Check targets to the right
                    targetBlocks[4] = Chunks.GetBlock(x + targetDirection[4], y);
                    targetAvailable[4] = targetBlocks[4] < block ? 1 : 0;
                    if (targetBlocks[4] <= block)
                    {
                        targetBlocks[5] = Chunks.GetBlock(x + targetDirection[5], y);
                        targetAvailable[5] = targetBlocks[5] < block ? 1 : 0;
                        if (targetBlocks[5] <= block)
                        {
                            targetBlocks[6] = Chunks.GetBlock(x + targetDirection[6], y);
                            targetAvailable[6] = targetBlocks[6] < block ? 1 : 0;
                            if (targetBlocks[6] <= block)
                            {
                                targetBlocks[7] = Chunks.GetBlock(x + targetDirection[7], y);
                                targetAvailable[7] = targetBlocks[7] < block ? 1 : 0;
                            }
                        }
                    }

                    var total = 0;
                    for (var i = 0; i < 8; ++i)
                        total += targetAvailable[i];
                    switch (total)
                    {
                        case 0: // could not do anything
                            return false;
                        case 8: // all blocks are available
                            index = _rng.Next(0, 8);
                            break;
                        default: // what could go wrong...
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

                            break;
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

            if (targetBlocks[0] < block) // available slot
            {
                firstTargetAvailable = true;
            }
            if (targetBlocks[1] < block &&
                (targetBlocks[0] < block || targetBlocks[0] == block))
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