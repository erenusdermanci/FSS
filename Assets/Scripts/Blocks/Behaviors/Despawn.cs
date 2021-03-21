using System;
using Chunks;

namespace Blocks.Behaviors
{
    public class Despawn : IBehavior
    {
        private const int Id = 2;

        public int GetId => Id;

        private readonly float _despawnProbability;
        private readonly float _lifetime;
        private readonly float _despawnResultBlockProbability;
        private readonly int _despawnResultBlockType;

        public Despawn(float despawnProbability,
            float lifetime,
            float despawnResultBlockProbability,
            int despawnResultBlockType)
        {
            _despawnProbability = despawnProbability;
            _lifetime = lifetime;
            _despawnResultBlockProbability = despawnResultBlockProbability;
            _despawnResultBlockType = despawnResultBlockType;
        }

        public bool Execute(Random rng, ChunkNeighborhood chunkNeighborhood, ChunkServer.BlockInfo blockInfo, int x, int y, ref bool destroyed)
        {
            if (blockInfo.Lifetime < _lifetime)
            {
                blockInfo.Lifetime += 1.0f;
                chunkNeighborhood.GetCentralChunk().SetBlockLifetime(x, y, blockInfo.Lifetime);
            }

            else
            {
                var despawnProbability = _despawnProbability;
                if (despawnProbability >= 1.0f
                    || despawnProbability > rng.NextDouble())
                {
                    // Destroy it
                    var resultBlockType = BlockConstants.Air;
                    if (_despawnResultBlockProbability > 0.0f
                        && _despawnResultBlockProbability >= 1.0f
                        || _despawnResultBlockProbability > rng.NextDouble())
                    {
                        resultBlockType = _despawnResultBlockType;
                    }
                    chunkNeighborhood.ReplaceBlock(x, y, resultBlockType,
                        BlockConstants.BlockDescriptors[resultBlockType].InitialStates,
                        BlockConstants.BlockDescriptors[resultBlockType].BaseHealth, 0);
                    destroyed = true;
                    return true;
                }
            }

            return true;
        }
    }
}
