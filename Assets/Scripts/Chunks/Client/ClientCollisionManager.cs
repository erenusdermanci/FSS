using System.Collections.Concurrent;
using DebugTools;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

namespace Chunks.Client
{
    public class ClientCollisionManager
    {
        private readonly ConcurrentDictionary<Vector2i, ClientCollisionTask> _clientCollisionTasks
            = new ConcurrentDictionary<Vector2i, ClientCollisionTask>();

        private ChunkNeighborhood<ChunkClient> _playerChunkNeighborhood;

        private readonly ChunkLayer _chunkLayer;

        public ClientCollisionManager(ChunkLayer chunkLayer)
        {
            _chunkLayer = chunkLayer;
        }

        public void GenerateCollisions(Vector2i playerFlooredPosition,
            bool playerHasMoved)
        {
            if (_chunkLayer.ClientChunkMap[playerFlooredPosition] == null)
                return;

            if (_playerChunkNeighborhood == null || playerHasMoved)
            {
                _playerChunkNeighborhood = new ChunkNeighborhood<ChunkClient>(_chunkLayer.ClientChunkMap,
                    _chunkLayer.ClientChunkMap[playerFlooredPosition]);
            }

            foreach (var chunk in _playerChunkNeighborhood.GetChunks())
            {
                if (chunk == null
                    || !GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks && !chunk.Dirty
                    && chunk.Collider.enabled)
                    continue;

                var task = new ClientCollisionTask(chunk, _chunkLayer.type);
                _clientCollisionTasks.TryAdd(chunk.Position, task);
            }

            foreach (var task in _clientCollisionTasks.Values)
            {
                task.Schedule();
            }

            foreach (var task in _clientCollisionTasks.Values)
            {
                if (!task.Scheduled())
                    continue;
                task.Join();

                var chunkCollider = task.Chunk.Collider;

                chunkCollider.enabled = false;
                chunkCollider.pathCount = task.CollisionData.Count;

                for (var i = 0; i < task.CollisionData.Count; ++i)
                {
                    var coll = task.CollisionData[i];
                    var vec2S = new Vector2[coll.Count];
                    for (var j = 0; j < coll.Count; ++j)
                    {
                        var x = coll[j].x / (float)Chunk.Size;
                        var y = coll[j].y / (float)Chunk.Size;
                        x -= 0.5f;
                        y -= 0.5f;
                        vec2S[j] = new Vector2(x, y);
                    }

                    chunkCollider.SetPath(i, vec2S);
                }

                chunkCollider.enabled = true;
            }

            _clientCollisionTasks.Clear();
        }

        public void QueueChunkCollisionGeneration(ChunkClient chunkClient)
        {
            if (_clientCollisionTasks.ContainsKey(chunkClient.Position))
                return;
            var task = new ClientCollisionTask(chunkClient, _chunkLayer.type);
            _clientCollisionTasks.TryAdd(chunkClient.Position, task);
        }
    }
}
