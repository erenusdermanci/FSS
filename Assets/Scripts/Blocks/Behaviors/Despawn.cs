using System;
using Chunks;

namespace Blocks.Behaviors
{
    public class Despawn : IBehavior
    {
        public const int Id = 2;

        public int GetId => Id;

        private readonly float _despawnProbability;
        private readonly float _lifetime;
        private readonly int _despawnResultBlockType;

        public Despawn(float despawnProbability,
            float lifetime,
            int despawnResultBlockType)
        {
            _despawnProbability = despawnProbability;
            _lifetime = lifetime;
            _despawnResultBlockType = despawnResultBlockType;
        }

        public bool Execute(Random rng, ChunkNeighborhood chunkNeighborhood, Chunk.BlockInfo blockInfo, int x, int y, ref bool destroyed)
        {
            var currentLifetime = blockInfo.Lifetime;
            if (currentLifetime < _lifetime)
                chunkNeighborhood.SetBlockLifetime(x, y, currentLifetime + 1.0f);
            else
            {
                var despawnProbability = _despawnProbability;
                if (despawnProbability >= 1.0f
                    || despawnProbability > rng.NextDouble())
                {
                    // Destroy it
                    chunkNeighborhood.PutBlock(x, y, _despawnResultBlockType,
                        BlockConstants.BlockDescriptors[_despawnResultBlockType].InitialStates,
                        BlockConstants.BlockDescriptors[_despawnResultBlockType].BaseHealth);
                    destroyed = true;
                    return true;
                }
            }

            return true;
        }
    }
}