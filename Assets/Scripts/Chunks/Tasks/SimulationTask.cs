using System;
using System.Collections.Generic;
using Blocks;
using DebugTools;
using Utils;

namespace Chunks.Tasks
{
    public class SimulationTask : ChunkTask
    {
        internal ChunkServerNeighborhood Chunks;

        private Random _rng;

        private static KnuthShuffle _noDirtyRectShuffle;

        private static KnuthShuffle[,] _dirtyRectShuffles;

        static SimulationTask()
        {
            ResetKnuthShuffle();
        }

        public SimulationTask(ChunkServer chunk) : base(chunk)
        {
        }

        public static void ResetKnuthShuffle()
        {
            if (!GlobalDebugConfig.StaticGlobalConfig.disableDirtyRects)
                InitializeDirtyRectShuffles();
            else
                InitializeNoDirtyRectShuffle();
        }

        private static void InitializeNoDirtyRectShuffle()
        {
            _noDirtyRectShuffle = new KnuthShuffle(new Random().Next(), global::Chunks.Chunk.Size * global::Chunks.Chunk.Size);
        }

        private static void InitializeDirtyRectShuffles()
        {
            var rng = new Random();
            var sizes = new SortedSet<int>();
            for (var x = 0; x < 32; ++x)
            {
                for (var y = 0; y < 32; ++y)
                {
                    sizes.Add((x + 1) * (y + 1));
                }
            }

            _dirtyRectShuffles = new KnuthShuffle[4, sizes.Max + 1];
            for (var i = 0; i < 4; ++i)
            {
                foreach (var size in sizes)
                {
                    _dirtyRectShuffles[i, size] = new KnuthShuffle(rng.Next(), size);
                }
            }
        }

        protected override unsafe void Execute()
        {
            #region stackalloc declared at the top for performance

            var distances = stackalloc int[64]
            {
                0, 0, 0, 0, // should not happen
                1, 0, 0, 0,
                2, 0, 0, 0,
                1, 2, 0, 0,
                3, 0, 0, 0,
                1, 3, 0, 0,
                2, 3, 0, 0,
                1, 2, 3, 0,
                4, 0, 0, 0,
                1, 4, 0, 0,
                2, 4, 0, 0,
                1, 2, 4, 0,
                3, 4, 0, 0,
                1, 3, 4, 0,
                2, 3, 4, 0,
                1, 2, 3, 4
            };
            var bitCount = stackalloc int[16] {0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4};
            var directionX = stackalloc int[] {0, -1, 1, -1, 1, 0, -1, 1};
            var directionY = stackalloc int[] {-1, -1, -1, 0, 0, 1, 1, 1};

            #endregion

            _rng = StaticRandom.Get();

            var blockInfo = new Block();
            var dirtied = false;

            if (GlobalDebugConfig.StaticGlobalConfig.disableDirtyRects)
            {
                const int totalSize = global::Chunks.Chunk.Size * global::Chunks.Chunk.Size;
                for (var i = 0; i < totalSize; ++i)
                {
                    var x = _noDirtyRectShuffle[i] / global::Chunks.Chunk.Size;
                    var y = _noDirtyRectShuffle[i] % global::Chunks.Chunk.Size;

                    dirtied |= SimulateBlock(x, y, ref blockInfo, distances, bitCount, directionX, directionY);
                }
            }
            else
            {
                for (var i = 0; i < Chunk.DirtyRects.Length; ++i)
                {
                    int startX, startY, endX, endY;
                    // First time we calculate the dirty rect, loop over all chunk
                    if (!Chunk.DirtyRects[i].Initialized)
                    {
                        startX = ChunkServer.DirtyRectX[i];
                        startY = ChunkServer.DirtyRectY[i];
                        endX = ChunkServer.DirtyRectX[i] + global::Chunks.Chunk.Size / 2 - 1;
                        endY = ChunkServer.DirtyRectY[i] + global::Chunks.Chunk.Size / 2 - 1;
                        Chunk.DirtyRects[i].Initialized = true;
                    }
                    // rect was initialized and reset but is empty
                    else if (Chunk.DirtyRects[i].X < 0)
                        continue;
                    // The dirty rect is initialized, loop over it and reset it
                    else
                    {
                        startX = ChunkServer.DirtyRectX[i] + Chunk.DirtyRects[i].X;
                        startY = ChunkServer.DirtyRectY[i] + Chunk.DirtyRects[i].Y;
                        endX = ChunkServer.DirtyRectX[i] + Chunk.DirtyRects[i].XMax;
                        endY = ChunkServer.DirtyRectY[i] + Chunk.DirtyRects[i].YMax;
                        Chunk.DirtyRects[i].Reset();
                    }

                    var knuthRngIndex = _rng.Next(0, 4);
                    var totalSize = (endX - startX + 1) * (endY - startY + 1);
                    for (var j = 0; j < totalSize; ++j)
                    {
                        var x = startX + _dirtyRectShuffles[knuthRngIndex, totalSize][j] % (endX - startX + 1);
                        var y = startY + _dirtyRectShuffles[knuthRngIndex, totalSize][j] / (endX - startX + 1);

                        dirtied |= SimulateBlock(x, y, ref blockInfo, distances, bitCount, directionX, directionY);
                    }
                }
            }

            Chunk.Dirty = dirtied;
        }

        private unsafe bool SimulateBlock(int x, int y, ref Block block,
            int* distances, int* bitCount, int* directionX, int* directionY)
        {
            if (Chunk.BlockUpdatedFlags[y * global::Chunks.Chunk.Size + x] == ChunkManager.UpdatedFlag)
            {
                Chunk.UpdateBlockDirty(x, y);
                return true;
            }

            Chunks.GetBlockInfo(x, y, ref block);

            var blockLogic = BlockConstants.BlockDescriptors[block.Type];

            var destroyed = false;
            var dirtied = false;
            if (blockLogic.Consumer != null)
            {
                dirtied |= blockLogic.Consumer.Execute(_rng, Chunks, x, y, directionX, directionY, ref destroyed);
            }
            if (!destroyed && blockLogic.FireSpreader != null)
            {
                dirtied |= blockLogic.FireSpreader.Execute(_rng, Chunks, block, x, y, directionX, directionY,
                    ref destroyed);
            }
            if (!destroyed && blockLogic.Despawner != null)
            {
                dirtied |= blockLogic.Despawner.Execute(_rng, Chunks, block, x, y, ref destroyed);
            }
            if (!destroyed && blockLogic.Swapper != null)
            {
                dirtied |= blockLogic.Swapper.Execute(_rng, Chunks, block.Type, x, y, directionX, directionY,
                    distances, bitCount);
            }

            return dirtied;
        }
    }
}
