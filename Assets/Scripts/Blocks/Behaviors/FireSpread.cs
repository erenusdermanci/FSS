using System;
using Chunks;
using Utils;

namespace Blocks.Behaviors
{
    public class FireSpread : IBehavior
    {
        public readonly float CombustionProbability;
        public readonly Color FireColor;

        private readonly float _burningRate;
        private readonly int[] _combustionEmissionBlockTypes;
        private readonly float[] _combustionEmissionProbabilities;
        private readonly int _combustionResultBlockType;
        private readonly float _combustionResultProbability;
        private readonly bool _selfExtinguishing;
        private readonly bool _destroyedWhenExtinguished;

        private static readonly float[] BurningRateMultipliers = {1f, 0.2f, 0.2f, 1f, 1f, 1f, 0.2f, 0.2f};

        public FireSpread(float combustionProbability,
            Color fireColor,
            float burningRate,
            int[] combustionEmissionBlockTypes,
            float[] combustionEmissionProbabilities,
            int combustionResultBlockType,
            float combustionResultProbability,
            bool selfExtinguishing,
            bool destroyedWhenExtinguished)
        {
            CombustionProbability = combustionProbability;
            FireColor = fireColor;
            _burningRate = burningRate;
            _combustionEmissionBlockTypes = combustionEmissionBlockTypes;
            _combustionEmissionProbabilities = combustionEmissionProbabilities;
            _combustionResultBlockType = combustionResultBlockType;
            _combustionResultProbability = combustionResultProbability;
            _selfExtinguishing = selfExtinguishing;
            _destroyedWhenExtinguished = destroyedWhenExtinguished;
        }

        public unsafe bool Execute(Random rng, ChunkNeighborhood chunkNeighborhood, ChunkServer.BlockInfo blockInfo, int x, int y, int* directionX,
            int* directionY, ref bool destroyed)
        {
            if (!blockInfo.GetState((int) BlockStates.Burning))
                return false;

            var neighborBlocks = stackalloc ChunkServer.BlockInfo[8];
            var airNeighborsCount = 0;
            var selfNeighborsCount = 0;
            var lavaNeighborsCount = 0;

            // We need to go through the neighbours of this fire block
            for (var i = 0; i < 8; ++i)
            {
                var neighborFound = chunkNeighborhood.GetBlockInfo(x + directionX[i], y + directionY[i], ref neighborBlocks[i]);

                if (!neighborFound)
                    continue;

                switch (neighborBlocks[i].Type)
                {
                    case BlockConstants.Air:
                        airNeighborsCount++;
                        break;
                    case BlockConstants.Flame:
                        break;
                    case BlockConstants.Lava:
                        lavaNeighborsCount++;
                        break;
                }

                if (neighborBlocks[i].Type == blockInfo.Type)
                    selfNeighborsCount++;
            }

            // We have our neighbor's types and our air count
            if (_burningRate > 0.0f
                && lavaNeighborsCount == 0
                && airNeighborsCount + (_selfExtinguishing ? 0 : selfNeighborsCount) == 0)
            {
                // fire dies out
                blockInfo.ClearState((int)BlockStates.Burning);
                if (_destroyedWhenExtinguished)
                {
                    return DestroyBlock(rng, chunkNeighborhood, x, y, ref destroyed);
                }

                // update state for block
                chunkNeighborhood.GetCentralChunk().SetBlockStates(x, y, blockInfo.StateBitset);

                // reset color of block
                var color = BlockConstants.BlockDescriptors[blockInfo.Type].Color;
                color.Shift(out var r, out var g, out var b);
                chunkNeighborhood.GetCentralChunk()
                    .SetBlockColor(x, y, r, g, b, color.a);
            }
            else
            {
                // now we try to spread
                for (var i = 0; i < 8; i++)
                {
                    switch (neighborBlocks[i].Type)
                    {
                        case -1: // there is no neighbour here (chunk doesn't exist)
                            break;
                        case BlockConstants.Air:
                            // replace Air with CombustionEmissionBlockType
                            if (_combustionEmissionProbabilities.Length == 0)
                                continue;
                            var roll = -1.0;
                            var blockToEmit = BlockConstants.Air;
                            for (var j = 0; j < _combustionEmissionProbabilities.Length; ++j)
                            {
                                // block probability is 0%, skip it
                                if (_combustionEmissionProbabilities[j] <= 0.0f)
                                    continue;

                                // block probability is 100%, so choose it
                                if (_combustionEmissionProbabilities[j] >= 1.0f)
                                {
                                    blockToEmit = _combustionEmissionBlockTypes[j];
                                    break;
                                }

                                // one roll to rule them all
                                if (roll < 0.0f)
                                    roll = rng.NextDouble();

                                if (_combustionEmissionProbabilities[j] >= roll)
                                {
                                    blockToEmit = _combustionEmissionBlockTypes[j];
                                    break;
                                }
                            }
                            chunkNeighborhood.ReplaceBlock(x + directionX[i], y + directionY[i], blockToEmit,
                                BlockConstants.BlockDescriptors[blockToEmit].InitialStates,
                                BlockConstants.BlockDescriptors[blockToEmit].BaseHealth, 0);
                            break;
                        default:
                            if (neighborBlocks[i].GetState((int)BlockStates.Burning))
                                continue;
                            var fireSpread = BlockConstants.BlockDescriptors[neighborBlocks[i].Type].FireSpread;
                            if (fireSpread == null)
                                continue;
                            var combustionProbability = fireSpread.CombustionProbability;
                            combustionProbability *= BurningRateMultipliers[i];
                            if (combustionProbability == 0.0f)
                                continue;
                            if (combustionProbability >= 1.0f
                                || combustionProbability > rng.NextDouble())
                            {
                                // spreading to this block
                                neighborBlocks[i].SetState((int)BlockStates.Burning);
                                var color = fireSpread.FireColor;
                                color.Shift(out var r, out var g, out var b);
                                chunkNeighborhood.UpdateBlock(x + directionX[i], y + directionY[i], neighborBlocks[i], r, g, b, color.a);
                            }
                            break;
                    }
                }

                blockInfo.Health -= _burningRate * (1 + airNeighborsCount + lavaNeighborsCount * 30);
                if (blockInfo.Health <= 0.0f)
                {
                    // Block is consumed by fire, destroy it
                    return DestroyBlock(rng, chunkNeighborhood, x, y, ref destroyed);
                }

                chunkNeighborhood.GetCentralChunk().SetBlockHealth(x, y, blockInfo.Health);
            }

            return true;
        }

        private bool DestroyBlock(Random rng, ChunkNeighborhood chunkNeighborhood, int x, int y, ref bool destroyed)
        {
            var combustionResultProbability = _combustionResultProbability;
            var resultBlockType = BlockConstants.Air;

            if (combustionResultProbability >= 1.0f
                || combustionResultProbability > rng.NextDouble())
                resultBlockType = _combustionResultBlockType;

            chunkNeighborhood.ReplaceBlock(x, y, resultBlockType,
                BlockConstants.BlockDescriptors[resultBlockType].InitialStates,
                BlockConstants.BlockDescriptors[resultBlockType].BaseHealth, 0);
            destroyed = true;
            return true;
        }
    }
}
