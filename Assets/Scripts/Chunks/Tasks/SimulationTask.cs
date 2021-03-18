using System;
using System.Collections.Generic;
using System.Threading;
using Blocks;
using Blocks.Behaviors;
using DebugTools;
using Utils;

namespace Chunks.Tasks
{
    public class SimulationTask : ChunkTask
    {
        internal ChunkNeighborhood Chunks;

        public ThreadLocal<Random> Random;

        private Random _rng;

        private readonly KnuthShuffle _noDirtyRectShuffle;

        private static readonly KnuthShuffle[,] DirtyRectShuffles;

        static SimulationTask()
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

            DirtyRectShuffles = new KnuthShuffle[4, sizes.Max + 1];
            for (var i = 0; i < 4; ++i)
            {
                foreach (var size in sizes)
                {
                    DirtyRectShuffles[i, size] = new KnuthShuffle(rng.Next(), size);
                }
            }
        }

        public SimulationTask(ChunkServer chunk) : base(chunk)
        {
            var rng = new Random();
            _noDirtyRectShuffle = new KnuthShuffle(rng.Next(), global::Chunks.Chunk.Size * global::Chunks.Chunk.Size);
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

            _rng = Random.Value;

            var blockInfo = new ChunkServer.BlockInfo();
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
                var shuffledDirtyRectOrder = new KnuthShuffle(_rng.Next(), Chunk.DirtyRects.Length);
                // need to randomize the dirtyrect traversal order...
                for (var i = 0; i < Chunk.DirtyRects.Length; ++i)
                {
                    var shuffledIndex = shuffledDirtyRectOrder[i];
                    int startX, startY, endX, endY;
                    // First time we calculate the dirty rect, loop over all chunk
                    if (Chunk.DirtyRects[shuffledIndex].X < 0)
                    {
                        startX = ChunkServer.DirtyRectX[shuffledIndex];
                        startY = ChunkServer.DirtyRectY[shuffledIndex];
                        endX = ChunkServer.DirtyRectX[shuffledIndex] + global::Chunks.Chunk.Size / 2 - 1;
                        endY = ChunkServer.DirtyRectY[shuffledIndex] + global::Chunks.Chunk.Size / 2 - 1;
                    }
                    // We already have a dirty rect, loop over it and reset it
                    else
                    {
                        startX = ChunkServer.DirtyRectX[shuffledIndex] + Chunk.DirtyRects[shuffledIndex].X;
                        startY = ChunkServer.DirtyRectY[shuffledIndex] + Chunk.DirtyRects[shuffledIndex].Y;
                        endX = ChunkServer.DirtyRectX[shuffledIndex] + Chunk.DirtyRects[shuffledIndex].XMax;
                        endY = ChunkServer.DirtyRectY[shuffledIndex] + Chunk.DirtyRects[shuffledIndex].YMax;
                        Chunk.DirtyRects[shuffledIndex].Reset();
                    }

                    var knuthRngIndex = _rng.Next(0, 4);
                    var totalSize = (endX - startX + 1) * (endY - startY + 1);
                    for (var j = 0; j < totalSize; ++j)
                    {
                        var x = startX + DirtyRectShuffles[knuthRngIndex, totalSize][j] % (endX - startX + 1);
                        var y = startY + DirtyRectShuffles[knuthRngIndex, totalSize][j] / (endX - startX + 1);

                        dirtied |= SimulateBlock(x, y, ref blockInfo, distances, bitCount, directionX, directionY);
                    }
                }
            }

            Chunk.Dirty = dirtied;
        }

        private unsafe bool SimulateBlock(int x, int y, ref ChunkServer.BlockInfo blockInfo,
            int* distances, int* bitCount, int* directionX, int* directionY)
        {
            if (Chunk.BlockUpdatedFlags[y * global::Chunks.Chunk.Size + x] == ChunkManager.UpdatedFlag)
            {
                Chunk.UpdateBlockDirty(x, y);
                return true;
            }

            Chunks.GetBlockInfo(x, y, ref blockInfo);

            var blockLogic = BlockConstants.BlockDescriptors[blockInfo.Type];

            var destroyed = false;
            var dirtied = false;
            foreach (var behavior in blockLogic.Behaviors)
            {
                if (destroyed)
                    break;
                switch (behavior.GetId)
                {
                    case Swap.Id:
                        dirtied |= ((Swap) behavior).Execute(_rng, Chunks, blockInfo.Type, x, y, directionX, directionY, distances, bitCount);
                        break;
                    case FireSpread.Id:
                        dirtied |= ((FireSpread) behavior).Execute(_rng, Chunks, blockInfo, x, y, directionX, directionY, ref destroyed);
                        break;
                    case Despawn.Id:
                        dirtied |= ((Despawn) behavior).Execute(_rng, Chunks, blockInfo, x, y, ref destroyed);
                        break;
                }
            }

            return dirtied;
        }
    }
}
