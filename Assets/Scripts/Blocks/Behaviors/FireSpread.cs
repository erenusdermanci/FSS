using System;
using Chunks;
using Utils;

namespace Blocks.Behaviors
{
    public readonly struct FireSpread : IBehavior
    {
        public const int Id = 1;

        public int GetId => Id;

        private readonly float _burningRate;
        private readonly int _combustionEmissionBlockType;
        private readonly float _combustionEmissionProbability;
        private readonly int _combustionResultBlockType;
        private readonly float _combustionResultProbability;
        private readonly bool _selfExtinguishing;
        private readonly bool _destroyedWhenExtinguished;

        private static readonly float[] BurningRateMultipliers = {1f, 0.2f, 0.2f, 1f, 1f, 1f, 0.2f, 0.2f};

        public FireSpread(float burningRate,
            int combustionEmissionBlockType,
            float combustionEmissionProbability,
            int combustionResultBlockType,
            float combustionResultProbability,
            bool selfExtinguishing,
            bool destroyedWhenExtinguished)
        {
            _burningRate = burningRate;
            _combustionEmissionBlockType = combustionEmissionBlockType;
            _combustionEmissionProbability = combustionEmissionProbability;
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

            var neighborTypes = stackalloc ChunkServer.BlockInfo[8];
            var airNeighborsCount = 0;
            var selfNeighborsCount = 0;

            // We need to go through the neighbours of this fire block
            for (var i = 0; i < 8; ++i)
            {
                var neighborFound = chunkNeighborhood.GetBlockInfo(x + directionX[i], y + directionY[i], ref neighborTypes[i]);

                if (!neighborFound)
                    continue;

                if (neighborTypes[i].Type == BlockConstants.Air)
                    airNeighborsCount++;
                else if (neighborTypes[i].Type == blockInfo.Type)
                    selfNeighborsCount++;
            }

            // We have our neighbor's types and our air count
            if (airNeighborsCount + (_selfExtinguishing ? 0 : selfNeighborsCount) == 0)
            {
                // fire dies out
                blockInfo.ClearState((int)BlockStates.Burning);
                if (_destroyedWhenExtinguished)
                {
                    return DestroyBlock(rng, chunkNeighborhood, x, y, ref destroyed);
                }

                // update state for block:
                chunkNeighborhood.GetCentralChunk().SetBlockStates(x, y, blockInfo.StateBitset);

                // reset color of block:
                var shiftAmount = Helpers.GetRandomShiftAmount(BlockConstants.BlockDescriptors[blockInfo.Type].ColorMaxShift);
                var color = BlockConstants.BlockDescriptors[blockInfo.Type].Color;
                chunkNeighborhood.GetCentralChunk()
                    .SetBlockColor(x, y,
                        Helpers.ShiftColorComponent(color.r, shiftAmount),
                        Helpers.ShiftColorComponent(color.g, shiftAmount),
                        Helpers.ShiftColorComponent(color.b, shiftAmount),
                        color.a);
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
                            var combustionEmissionProbability = _combustionEmissionProbability;
                            if (combustionEmissionProbability <= 0.0f)
                                continue;
                            if (combustionEmissionProbability >= 1.0f
                                || combustionEmissionProbability > rng.NextDouble())
                            {
                                chunkNeighborhood.ReplaceBlock(x + directionX[i], y + directionY[i], _combustionEmissionBlockType,
                                    BlockConstants.BlockDescriptors[_combustionEmissionBlockType].InitialStates,
                                    BlockConstants.BlockDescriptors[_combustionEmissionBlockType].BaseHealth, 0);
                            }
                            break;
                        default:
                            if (neighborTypes[i].GetState((int)BlockStates.Burning))
                                continue;
                            var combustionProbability =
                                BlockConstants.BlockDescriptors[neighborTypes[i].Type].CombustionProbability
                                * BurningRateMultipliers[i];
                            if (combustionProbability == 0.0f)
                                continue;
                            if (combustionProbability >= 1.0f
                                || combustionProbability > rng.NextDouble())
                            {
                                // spreading to this block
                                neighborTypes[i].SetState((int)BlockStates.Burning);
                                var shiftAmount = Helpers.GetRandomShiftAmount(BlockConstants.FireColorMaxShift);
                                var color = BlockConstants.FireColor;
                                chunkNeighborhood.UpdateBlock(x + directionX[i], y + directionY[i], neighborTypes[i],
                                    Helpers.ShiftColorComponent(color.r, shiftAmount),
                                    Helpers.ShiftColorComponent(color.g, shiftAmount),
                                    Helpers.ShiftColorComponent(color.b, shiftAmount),
                                    color.a);
                            }
                            break;
                    }
                }

                blockInfo.Health -= _burningRate * (1 + airNeighborsCount);
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
