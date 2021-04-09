using System.Collections.Generic;
using Chunks.Collision;
using Chunks.Tasks;
using Utils;

namespace Chunks.Client
{
    public class ClientCollisionTask : ChunkTask<ChunkClient>
    {
        public List<List<Vector2i>> CollisionData;

        public ClientCollisionTask(ChunkClient chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            CollisionData = ChunkCollision.ComputeChunkColliders(Chunk);
        }
    }
}
