using System;
using Chunks.Server;

namespace Blocks.Behaviors
{
    public class Despawner : IBehavior
    {
        private readonly float _despawnProbability;
        private readonly float _lifetime;
        private readonly BlockPotential _resultPotentialBlocks;

        public Despawner(float despawnProbability,
            float lifetime,
            BlockPotential resultPotentialBlocks)
        {
            _despawnProbability = despawnProbability;
            _lifetime = lifetime;
            _resultPotentialBlocks = resultPotentialBlocks;
        }

        public bool Execute(Random rng, ChunkServerNeighborhood chunkNeighborhood, ref Block block, int x, int y, ref bool destroyed)
        {
            if (block.lifetime < _lifetime)
            {
                block.lifetime += 1.0f;
                chunkNeighborhood.GetCentralChunk().UpdateBlockDirty(x, y, block.type);
            }
            else
            {
                if (_despawnProbability < 1.0f && _despawnProbability <= rng.NextDouble())
                    return true;
                // Destroy it
                var resultBlockType = BlockConstants.Air;
                if (_resultPotentialBlocks.Probability > 0.0f
                    && _resultPotentialBlocks.Probability >= 1.0f
                    || _resultPotentialBlocks.Probability > rng.NextDouble())
                {
                    resultBlockType = _resultPotentialBlocks.Type;
                }
                chunkNeighborhood.ReplaceBlock(x, y, resultBlockType,
                    BlockConstants.BlockDescriptors[resultBlockType].InitialStates,
                    BlockConstants.BlockDescriptors[resultBlockType].BaseHealth, 0, 0);
                destroyed = true;
                return true;
            }

            return true;
        }
    }
}
