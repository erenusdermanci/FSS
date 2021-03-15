using System;
using System.Threading;
using Blocks;
using Blocks.Behaviors;

namespace Chunks.Tasks
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

            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            var blockMoveInfo = new BlockMoveInfo();
            Chunk.DirtyRect.x = -1;
            Chunk.DirtyRect.y = -1;
            Chunk.DirtyRect.xMax = -1;
            Chunk.DirtyRect.yMax = -1;
            var blockInfo = new Chunk.BlockInfo();
            var dirtied = false;

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

                Chunks.GetBlockInfo(x, y, ref blockInfo);

                var blockLogic = BlockConstants.BlockDescriptors[blockInfo.Type];

                var destroyed = false;
                foreach (var behavior in blockLogic.Behaviors)
                {
                    if (destroyed)
                        break;
                    switch (behavior.GetId)
                    {
                        case Swap.Id:
                            dirtied |= ((Swap) behavior).Execute(_rng, Chunks, blockInfo.Type, x, y, ref blockMoveInfo, directionX, directionY, distances, bitCount);
                            break;
                        case FireSpread.Id:
                            dirtied |= ((FireSpread) behavior).Execute(_rng, Chunks, blockInfo, x, y, directionX, directionY, ref destroyed);
                            break;
                        case Despawn.Id:
                            dirtied |= ((Despawn) behavior).Execute(_rng, Chunks, blockInfo, x, y, ref destroyed);
                            break;
                    }
                }

                if (blockMoveInfo.Chunk == 0)
                {
                    var c = Chunks[blockMoveInfo.Chunk];

                    if (c.DirtyRect.x < 0.0f)
                    {
                        if (c.DirtyRect.x < 0.0f) c.DirtyRect.x = blockMoveInfo.X;
                        if (c.DirtyRect.xMax < 0.0f) c.DirtyRect.xMax = blockMoveInfo.X;
                        if (c.DirtyRect.y < 0.0f) c.DirtyRect.y = blockMoveInfo.Y;
                        if (c.DirtyRect.yMax < 0.0f) c.DirtyRect.yMax = blockMoveInfo.Y;
                    }
                    else
                    {
                        if (c.DirtyRect.x > blockMoveInfo.X)
                            c.DirtyRect.x = blockMoveInfo.X;
                        if (c.DirtyRect.xMax < blockMoveInfo.X)
                            c.DirtyRect.xMax = blockMoveInfo.X;
                        if (c.DirtyRect.y > blockMoveInfo.Y)
                            c.DirtyRect.y = blockMoveInfo.Y;
                        if (c.DirtyRect.yMax < blockMoveInfo.Y)
                            c.DirtyRect.yMax = blockMoveInfo.Y;
                    }
                }

                if (blockMoveInfo.Chunk > 0)
                {
                    Chunks[blockMoveInfo.Chunk].Dirty = true;
                }
            }

            Chunk.Dirty = dirtied;

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
    }
}
