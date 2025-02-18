﻿using System;
using Chunks;
using Chunks.Server;
using Utils;

namespace Blocks.Behaviors
{
    public class FireSpreader : IBehavior
    {
        public readonly float CombustionProbability;
        public readonly Color FireColor;
        private readonly Color _burntColor;
        private readonly float _burningRate;
        private readonly BlockPotential[] _emissionPotentialBlocks;
        private readonly BlockPotential _combustionPotentialBlock;
        private readonly bool _selfExtinguishing;
        private readonly bool _destroyedWhenExtinguished;

        private static readonly float[] BurningRateMultipliers = {1f, 0.2f, 0.2f, 1f, 1f, 1f, 0.2f, 0.2f};

        public FireSpreader(float combustionProbability,
            Color fireColor,
            Color burntColor,
            float burningRate,
            BlockPotential[] emissionPotentialBlocks,
            BlockPotential combustionPotentialBlock,
            bool selfExtinguishing,
            bool destroyedWhenExtinguished)
        {
            CombustionProbability = combustionProbability;
            FireColor = fireColor;
            _burntColor = burntColor;
            _burningRate = burningRate;
            _emissionPotentialBlocks = emissionPotentialBlocks;
            _combustionPotentialBlock = combustionPotentialBlock;
            _selfExtinguishing = selfExtinguishing;
            _destroyedWhenExtinguished = destroyedWhenExtinguished;
        }

        public unsafe bool Execute(Random rng, ChunkServerNeighborhood chunkNeighborhood, ref Block block, int x, int y, int* directionX,
            int* directionY, ref bool destroyed)
        {
            if (!block.GetState((int) BlockStates.Burning))
                return false;

            var dirty = true;

            var neighborBlocks = stackalloc Block*[8];
            var airNeighborsCount = 0;
            var selfNeighborsCount = 0;
            var lavaNeighborsCount = 0;

            var chunks = chunkNeighborhood.GetChunks();
            for (var i = 0; i < 8; ++i)
            {
                var nx = x + directionX[i];
                var ny = y + directionY[i];
                ChunkServerNeighborhood.UpdateOutsideChunk(ref nx, ref ny, out var chunkIndex);
                var chunk = chunks[chunkIndex];
                if (chunk == null)
                {
                    neighborBlocks[i] = null;
                    continue;
                }

                fixed (Block *neighborBlock = &chunk.GetBlockInfo(ny * Chunk.Size + nx))
                {
                    neighborBlocks[i] = neighborBlock;
                }

                switch (neighborBlocks[i]->type)
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

                if (neighborBlocks[i]->type == block.type)
                    selfNeighborsCount++;
            }

            // We have our neighbor's types and our air count
            if (_burningRate > 0.0f
                && lavaNeighborsCount == 0
                && airNeighborsCount + (_selfExtinguishing ? 0 : selfNeighborsCount) == 0)
            {
                // fire dies out
                block.ClearState((int)BlockStates.Burning);
                if (_destroyedWhenExtinguished)
                {
                    return DestroyBlock(rng, chunkNeighborhood, x, y, ref destroyed);
                }

                // reset color of block
                chunkNeighborhood.GetCentralChunk().SetBlockColor(x, y, _burntColor);
                chunkNeighborhood.GetCentralChunk().UpdateBlockDirty(x, y, block.type);
            }
            else
            {
                // now we try to spread
                for (var i = 0; i < 8; i++)
                {
                    if (neighborBlocks[i] == null)
                        continue;
                    switch (neighborBlocks[i]->type)
                    {
                        case BlockConstants.Air:
                            // replace Air with CombustionEmissionBlockType
                            if (_emissionPotentialBlocks.Length == 0)
                                continue;
                            var roll = -1.0;
                            var blockToEmit = BlockConstants.Air;
                            foreach (var blockPotential in _emissionPotentialBlocks)
                            {
                                // block probability is 0%, skip it
                                if (blockPotential.Probability <= 0.0f)
                                    continue;

                                // block probability is 100%, so choose it
                                if (blockPotential.Probability >= 1.0f)
                                {
                                    blockToEmit = blockPotential.Type;
                                    break;
                                }

                                // one roll to rule them all
                                if (roll < 0.0f)
                                    roll = rng.NextDouble();

                                if (blockPotential.Probability >= roll)
                                {
                                    blockToEmit = blockPotential.Type;
                                    break;
                                }
                            }
                            chunkNeighborhood.ReplaceBlock(x + directionX[i], y + directionY[i], blockToEmit,
                                BlockConstants.BlockDescriptors[blockToEmit].InitialStates,
                                BlockConstants.BlockDescriptors[blockToEmit].BaseHealth, 0, 0);
                            break;
                        default:
                            if (neighborBlocks[i]->GetState((int)BlockStates.Burning))
                                continue;
                            var fireSpread = BlockConstants.BlockDescriptors[neighborBlocks[i]->type].FireSpreader;
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
                                neighborBlocks[i]->SetState((int)BlockStates.Burning);
                                var color = fireSpread.FireColor;
                                color.Shift(out var r, out var g, out var b);
                                chunkNeighborhood.UpdateBlock(x + directionX[i], y + directionY[i], neighborBlocks[i], r, g, b, color.a);
                            }
                            break;
                    }
                }

                var healthDecrement = _burningRate * (1 + airNeighborsCount + lavaNeighborsCount * 30);
                if (healthDecrement > 0.0f)
                {
                    block.health -= healthDecrement;
                    if (block.health <= 0.0f)
                    {
                        // Block is consumed by fire, destroy it
                        return DestroyBlock(rng, chunkNeighborhood, x, y, ref destroyed);
                    }

                    chunkNeighborhood.GetCentralChunk().UpdateBlockDirty(x, y, block.type);
                }

                dirty = healthDecrement > 0.0f;
            }

            return dirty;
        }

        private bool DestroyBlock(Random rng, ChunkServerNeighborhood chunkNeighborhood, int x, int y, ref bool destroyed)
        {
            var resultBlockType = BlockConstants.Air;
            if (_combustionPotentialBlock != null)
            {
                var combustionResultProbability = _combustionPotentialBlock.Probability;
                if (combustionResultProbability >= 1.0f
                    || combustionResultProbability > rng.NextDouble())
                    resultBlockType = _combustionPotentialBlock.Type;
            }

            chunkNeighborhood.ReplaceBlock(x, y, resultBlockType,
                BlockConstants.BlockDescriptors[resultBlockType].InitialStates,
                BlockConstants.BlockDescriptors[resultBlockType].BaseHealth, 0, 0);
            destroyed = true;
            return true;
        }
    }
}
