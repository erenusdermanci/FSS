using System.Collections.Generic;
using Chunks.Collision;
using Chunks.Server;
using Chunks.Tasks;
using UnityEngine;

namespace Chunks.Client
{
    public class ClientCollisionTask : ChunkTask<ChunkServer>
    {
        public List<List<Vector2>> CollisionData;

        public ClientCollisionTask(ChunkServer chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            CollisionData = ChunkCollision.ComputeChunkColliders(Chunk);
        }
    }
}
