using DataComponents;
using System.Threading;
using Unity.Collections;
using static Constants;

namespace ChunkTasks
{
    public class SimulationTask : IChunkTask
    {
        private readonly Chunk[] _chunks;
        private readonly NativeArray<Unity.Mathematics.Random> _randomArray;
        private Unity.Mathematics.Random _rng;

        public SimulationTask(Chunk[] chunks, NativeArray<Unity.Mathematics.Random> randomArray)
        {
            _chunks = chunks;
            _randomArray = randomArray;
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
                            SimulateOil(block);
                            break;
                        case (int)Blocks.Water:
                            SimulateWater(block);
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
                }
            }
        }

        #region Block Logic

        private void SimulateOil(int block)
        {

        }

        private void SimulateWater(int block)
        {

        }

        private unsafe void SimulateSand(int block, int x, int y)
        {
            // DOWN IS PRIORITY!
            int* downBlocks = stackalloc int[2];
            downBlocks[0] = GetBlock(x, y - 1);
            downBlocks[1] = GetBlock(x, y - 2);

            //var firstDown = GetBlock(x, y - 1);
            var firstDownAvailable = false;
            //var secondDown = GetBlock(x, y - 2);
            var secondDownAvailable = false;

            if (downBlocks[0] < block) // available slot
            {
                firstDownAvailable = true;
            }

            if (downBlocks[1] < block)
            {
                secondDownAvailable = true;
            }

            // start determining the index to put block @
            int index;
            if (firstDownAvailable && secondDownAvailable)
            {
                // random
                index = _rng.NextInt(0, 2);
            }
            else if (firstDownAvailable)
            {
                index = 0;
            }
            else if (secondDownAvailable)
            {
                index = 1;
            }
            else // none available
            {
                return;
            }

            PutBlock(x, y - (index + 1), block);
            PutBlock(x, y, downBlocks[index]);
        }

        //                    if (block == (int) Blocks.Sand)
        //{
        //    var downBlock = GetBlock(x, y - 1);
        //    var leftDownBlock = GetBlock(x - 1, y - 1);
        //    var rightDownBlock = GetBlock(x + 1, y - 1);
        //    if (block > downBlock)
        //    {
        //        PutBlock(x, y - 1, block);
        //        PutBlock(x, y, downBlock);
        //    }
        //    else if (block > leftDownBlock)
        //    {
        //        PutBlock(x - 1, y - 1, block);
        //        PutBlock(x, y, leftDownBlock);
        //    }
        //    else if (block > rightDownBlock)
        //    {
        //        PutBlock(x + 1, y - 1, block);
        //        PutBlock(x, y, rightDownBlock);
        //    }
        //}
        //                if (block == (int) Blocks.Water)
        //{
        //    var downBlock = GetBlock(x, y - 1);
        //    var leftDownBlock = GetBlock(x - 1, y - 1);
        //    var rightDownBlock = GetBlock(x + 1, y - 1);
        //    var leftBlock = GetBlock(x - 1, y);
        //    var rightBlock = GetBlock(x + 1, y);
        //    if (downBlock < block)
        //    {
        //        PutBlock(x, y - 1, block);
        //        PutBlock(x, y, downBlock);
        //    }
        //    else if (leftDownBlock < block)
        //    {
        //        PutBlock(x - 1, y - 1, block);
        //        PutBlock(x, y, leftDownBlock);
        //    }
        //    else if (rightDownBlock < block)
        //    {
        //        PutBlock(x + 1, y - 1, block);
        //        PutBlock(x, y, rightDownBlock);
        //    }
        //    else if (leftBlock < block)
        //    {
        //        PutBlock(x - 1, y, block);
        //        PutBlock(x, y, leftBlock);
        //    }
        //    else if (rightBlock < block)
        //    {
        //        PutBlock(x + 1, y, block);
        //        PutBlock(x, y, rightBlock);
        //    }
        //}
        //                if (block == (int) Blocks.Oil)
        //{
        //    var upBlock = GetBlock(x, y + 1);
        //    var upLeftBlock = GetBlock(x - 1, y + 1);
        //    var upRightBlock = GetBlock(x + 1, y + 1);
        //    var downBlock = GetBlock(x, y - 1);
        //    var leftDownBlock = GetBlock(x - 1, y - 1);
        //    var rightDownBlock = GetBlock(x + 1, y - 1);
        //    var leftBlock = GetBlock(x - 1, y);
        //    var rightBlock = GetBlock(x + 1, y);
        //    if (block < upBlock && upBlock < SolidThreshold)
        //    {
        //        PutBlock(x, y + 1, block);
        //        PutBlock(x, y, upBlock);
        //    }
        //    else if (block < upLeftBlock && upLeftBlock < SolidThreshold)
        //    {
        //        PutBlock(x - 1, y + 1, block);
        //        PutBlock(x, y, upLeftBlock);
        //    }
        //    else if (block < upRightBlock && upRightBlock < SolidThreshold)
        //    {
        //        PutBlock(x + 1, y + 1, block);
        //        PutBlock(x, y, upRightBlock);
        //    }
        //    else if (downBlock < block)
        //    {
        //        PutBlock(x, y - 1, block);
        //        PutBlock(x, y, downBlock);
        //    }
        //    else if (leftDownBlock < block)
        //    {
        //        PutBlock(x - 1, y - 1, block);
        //        PutBlock(x, y, leftDownBlock);
        //    }
        //    else if (rightDownBlock < block)
        //    {
        //        PutBlock(x + 1, y - 1, block);
        //        PutBlock(x, y, rightDownBlock);
        //    }
        //    else if (leftBlock < block)
        //    {
        //        PutBlock(x - 1, y, block);
        //        PutBlock(x, y, leftBlock);
        //    }
        //    else if (rightBlock < block)
        //    {
        //        PutBlock(x + 1, y, block);
        //        PutBlock(x, y, rightBlock);
        //    }

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