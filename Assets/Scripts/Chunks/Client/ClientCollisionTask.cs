using System.Collections.Generic;
using Chunks.Collision;
using Chunks.Tasks;
using Utils;
using static Chunks.ChunkLayer;

namespace Chunks.Client
{
    public class ClientCollisionTask : ChunkTask<ChunkClient>
    {
        public List<List<Vector2i>> CollisionData;

        public ClientCollisionTask(ChunkClient chunk, ChunkLayerType layerType) : base(chunk, layerType)
        {
        }

        protected override void Execute()
        {
            CollisionData = ChunkCollision.ComputeChunkColliders(Chunk);
        }
    }
}
