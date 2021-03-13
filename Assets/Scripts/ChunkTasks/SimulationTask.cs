﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using BlockBehavior;
using DataComponents;
using Utils;

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

                moved |= SimulateBlock(block, x, y, ref blockMoveInfo, distances, bitCount, directionX, directionY);

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
            int* distances, int* bitCount, int* directionX, int* directionY)
        {
            var blockLogic = BlockLogic.BlockDescriptors[block];

            foreach (var behavior in blockLogic.Behaviors)
            {
                switch (behavior.Id)
                {
                    case 0:
                        return Swap((Swap) behavior, block, x, y, ref blockMoveInfo, directionX, directionY, distances, bitCount);
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool Swap(Swap swap, int block, int x, int y, ref ChunkNeighborhood.BlockMoveInfo blockMoveInfo,
            int* directionX, int* directionY, int* distances, int* bitCount)
        {
            var availableTargets = stackalloc int[4];
            var targetBlocks = stackalloc int[4];

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
                            for (var j = 0; j < swap.Directions[directionIdx]; ++j)
                            {
                                if (availableTargets[j] != 1)
                                    continue;
                                distance = j + 1;
                                break;
                            }

                            break;
                        case MovementType.Farthest:
                            for (var j = swap.Directions[directionIdx] - 1; j >= 0; --j)
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

                    return Chunks.MoveBlock(x, y,
                        distance * directionX[directionIdx],
                        distance * directionY[directionIdx],
                        block, targetBlocks[distance - 1],
                        ref blockMoveInfo);
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool FillAvailableTargets(Swap swap, int x, int y, int block, int directionIdx,
            int* directionX, int* directionY, int* availableTargets, int* targetBlocks)
        {
            var targetsFound = false;
            for (var j = 0; j < swap.Directions[directionIdx]; ++j)
            {
                targetBlocks[j] = Chunks.GetBlock(x + (j + 1) * directionX[directionIdx], y + (j + 1) * directionY[directionIdx]);
                if (swap.BlockedBy == BlockLogic.BlockDescriptors[targetBlocks[j]].PhysicalTag)
                {
                    availableTargets[j] = 0;
                    return targetsFound;
                }

                if (!(BlockLogic.BlockDescriptors[targetBlocks[j]].Density < BlockLogic.BlockDescriptors[block].Density))
                    continue;
                availableTargets[j] = 1;
                targetsFound = true;
            }

            return targetsFound;
        }
    }
}