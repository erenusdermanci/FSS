using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Blocks;
using Blocks.Behaviors;
using Utils;
using static Chunks.ChunkNeighborhood;

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
            var blockData = new BlockData();
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

                Chunks.GetBlockData(x, y, ref blockData);

                var blockLogic = BlockConstants.BlockDescriptors[blockData.Type];

                var destroyed = false;
                foreach (var behavior in blockLogic.Behaviors)
                {
                    if (destroyed)
                        break;
                    switch (behavior.GetId)
                    {
                        case Blocks.Behaviors.Swap.Id:
                            dirtied |= Swap((Swap) behavior, blockData.Type, x, y, ref blockMoveInfo, directionX, directionY, distances, bitCount);
                            break;
                        case Blocks.Behaviors.FireSpread.Id:
                            if (!blockData.GetState((int) BlockStates.Burning))
                                break;
                            dirtied |= FireSpread((FireSpread) behavior, blockData, x, y, directionX, directionY, ref destroyed);
                            break;
                        case Blocks.Behaviors.Despawn.Id:
                            dirtied |= Despawn((Despawn) behavior, blockData, x, y, ref destroyed);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool Swap(Swap swap, int block, int x, int y, ref BlockMoveInfo blockMoveInfo,
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
                        case BlockMovementType.Closest:
                            for (var j = 0; j < swap.Directions[directionIdx]; ++j)
                            {
                                if (availableTargets[j] != 1)
                                    continue;
                                distance = j + 1;
                                break;
                            }

                            break;
                        case BlockMovementType.Farthest:
                            for (var j = swap.Directions[directionIdx] - 1; j >= 0; --j)
                            {
                                if (availableTargets[j] != 1)
                                    continue;
                                distance = j + 1;
                                break;
                            }

                            break;
                        case BlockMovementType.Randomized:
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
                if (swap.BlockedBy == BlockConstants.BlockDescriptors[targetBlocks[j]].Tag)
                {
                    availableTargets[j] = 0;
                    return targetsFound;
                }

                if (!(BlockConstants.BlockDescriptors[targetBlocks[j]].DensityPriority < BlockConstants.BlockDescriptors[block].DensityPriority))
                    continue;
                availableTargets[j] = 1;
                targetsFound = true;
            }

            return targetsFound;
        }

        private unsafe bool FireSpread(FireSpread behavior, BlockData blockData, int x, int y, int* directionX,
            int* directionY, ref bool destroyed)
        {
            var neighborTypes = stackalloc BlockData[8];
            var airNeighborsCount = 0;
            var selfNeighborsCount = 0;

            // We need to go through the neighbours of this fire block
            for (var i = 0; i < 8; ++i)
            {
                var neighborFound = Chunks.GetBlockData(x + directionX[i], y + directionY[i], ref neighborTypes[i]);

                if (!neighborFound)
                    continue;

                if (neighborTypes[i].Type == BlockConstants.Air)
                    airNeighborsCount++;
                else if (neighborTypes[i].Type == blockData.Type)
                    selfNeighborsCount++;
            }

            // We have our neighbor's types and our air count
            if (airNeighborsCount + (behavior.SelfExtinguishing ? 0 : selfNeighborsCount) == 0)
            {
                // fire dies out
                blockData.ClearState((int)BlockStates.Burning);
                Chunks.PutBlock(x, y, blockData.Type, blockData.StateBitset);
            }
            else
            {
                // now we try to spread
                for (var i = 0; i < 8; i++)
                {
                    switch (neighborTypes[i].Type)
                    {
                        case -1: // there is no neighbour here (chunk doesn't exist)
                            break;
                        case BlockConstants.Air:
                            // replace Air with CombustionEmissionBlockType
                            var combustionEmissionProbability = behavior.CombustionEmissionProbability;
                            if (combustionEmissionProbability == 0.0f)
                                continue;
                            if (combustionEmissionProbability >= 1.0f
                                || combustionEmissionProbability > _rng.NextDouble())
                            {
                                Chunks.PutBlock(x + directionX[i], y + directionY[i], behavior.CombustionEmissionBlockType,
                                    BlockConstants.BlockDescriptors[behavior.CombustionEmissionBlockType].InitialStates,
                                    BlockConstants.BlockDescriptors[behavior.CombustionEmissionBlockType].BaseHealth);
                            }
                            break;
                        default:
                            if (neighborTypes[i].GetState((int)BlockStates.Burning))
                                continue;
                            var combustionProbability =
                                BlockConstants.BlockDescriptors[neighborTypes[i].Type].CombustionProbability;
                            if (combustionProbability == 0.0f)
                                continue;
                            if (combustionProbability >= 1.0f
                                || combustionProbability > _rng.NextDouble())
                            {
                                // spreading to this block
                                neighborTypes[i].SetState((int)BlockStates.Burning);
                                var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockConstants.FireColorMaxShift);
                                var color = BlockConstants.FireColor;
                                Chunks.PutBlock(x + directionX[i], y + directionY[i], neighborTypes[i].Type,
                                    Helpers.ShiftColorComponent(color.r, shiftAmount),
                                    Helpers.ShiftColorComponent(color.g, shiftAmount),
                                    Helpers.ShiftColorComponent(color.b, shiftAmount),
                                    color.a,
                                    neighborTypes[i].StateBitset);
                            }
                            break;
                    }
                }

                var updatedHealth = blockData.Health - behavior.BurningRate * (1 + airNeighborsCount);
                if (updatedHealth <= 0.0f)
                {
                    // Block is consumed by fire, destroy it

                    var combustionResultProbability = behavior.CombustionResultProbability;
                    var resultBlockType = BlockConstants.Air;

                    if (combustionResultProbability >= 1.0f
                        || combustionResultProbability > _rng.NextDouble())
                        resultBlockType = behavior.CombustionResultBlockType;

                    Chunks.PutBlock(x, y, resultBlockType,
                        BlockConstants.BlockDescriptors[resultBlockType].InitialStates,
                        BlockConstants.BlockDescriptors[resultBlockType].BaseHealth);
                    destroyed = true;
                    return true;
                }

                Chunks.SetBlockHealth(x, y, updatedHealth);
            }

            return true;
        }

        private bool Despawn(Despawn behavior, BlockData blockData, int x, int y, ref bool destroyed)
        {
            var currentLifetime = blockData.Lifetime;
            if (currentLifetime < behavior.Lifetime)
                Chunks.SetBlockLifetime(x, y, currentLifetime + 1.0f);
            else
            {
                var despawnProbability = behavior.DespawnProbability;
                if (despawnProbability >= 1.0f
                    || despawnProbability > _rng.NextDouble())
                {
                    // Destroy it
                    Chunks.PutBlock(x, y, behavior.DespawnResultBlockType,
                        BlockConstants.BlockDescriptors[behavior.DespawnResultBlockType].InitialStates,
                        BlockConstants.BlockDescriptors[behavior.DespawnResultBlockType].BaseHealth);
                    destroyed = true;
                    return true;
                }
            }

            return true;
        }
    }
}
