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
            Chunk.DirtyRect.X = -1;
            Chunk.DirtyRect.Y = -1;
            Chunk.DirtyRect.XMax = -1;
            Chunk.DirtyRect.YMax = -1;
            var blockInfo = new Chunk.BlockInfo();
            var dirtied = false;

            const int totalSize = Chunk.Size * Chunk.Size;
            for (var i = 0; i < totalSize; ++i)
            {
                var x = _indexingOrder[i] / Chunk.Size;
                var y = _indexingOrder[i] % Chunk.Size;

                dirtied |= SimulateBlock(x, y, ref blockMoveInfo, ref blockInfo, distances, bitCount, directionX, directionY);
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

        private unsafe bool SimulateBlock(int x, int y, ref BlockMoveInfo blockMoveInfo, ref Chunk.BlockInfo blockInfo,
            int* distances, int* bitCount, int* directionX, int* directionY)
        {
            blockMoveInfo.Chunk = -1;
            blockMoveInfo.X = -1;
            blockMoveInfo.Y = -1;

            if (Chunks[0].BlockUpdatedFlags[y * Chunk.Size + x] == 1)
                return false;

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

                if (c.DirtyRect.X < 0.0f)
                {
                    if (c.DirtyRect.X < 0.0f) c.DirtyRect.X = blockMoveInfo.X;
                    if (c.DirtyRect.XMax < 0.0f) c.DirtyRect.XMax = blockMoveInfo.X;
                    if (c.DirtyRect.Y < 0.0f) c.DirtyRect.Y = blockMoveInfo.Y;
                    if (c.DirtyRect.YMax < 0.0f) c.DirtyRect.YMax = blockMoveInfo.Y;
                }
                else
                {
                    if (c.DirtyRect.X > blockMoveInfo.X)
                        c.DirtyRect.X = blockMoveInfo.X;
                    if (c.DirtyRect.XMax < blockMoveInfo.X)
                        c.DirtyRect.XMax = blockMoveInfo.X;
                    if (c.DirtyRect.Y > blockMoveInfo.Y)
                        c.DirtyRect.Y = blockMoveInfo.Y;
                    if (c.DirtyRect.YMax < blockMoveInfo.Y)
                        c.DirtyRect.YMax = blockMoveInfo.Y;
                }
            }

            if (blockMoveInfo.Chunk > 0)
            {
                Chunks[blockMoveInfo.Chunk].Dirty = true;
            }

            return dirtied;
        }
    }
}
