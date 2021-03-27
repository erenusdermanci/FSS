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

        public void GenerateCollisions(ChunkMap<ChunkClient> clientChunkMap,
            Vector2i playerFlooredPosition,
            bool playerHasMoved)
        {
            if (clientChunkMap[playerFlooredPosition] == null)
                return;

            if (_playerChunkNeighborhood == null || playerHasMoved)
            {
                _playerChunkNeighborhood = new ChunkNeighborhood<ChunkClient>(clientChunkMap,
                    clientChunkMap[playerFlooredPosition]);
            }

            foreach (var chunk in _playerChunkNeighborhood.GetChunks())
            {
                if (chunk == null
                    || !GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks && !chunk.Dirty
                    && chunk.Collider.enabled)
                    continue;

                var task = new ClientCollisionTask(chunk);
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

                        if (GlobalDebugConfig.StaticGlobalConfig.outlineChunkColliders)
                        {
                            if (j == 0)
                                continue;
                            var p1 = vec2S[j - 1];
                            var p2 = vec2S[j];
                            Debug.DrawLine(
                                new Vector2(task.Chunk.Position.x + p1.x, task.Chunk.Position.y + p1.y),
                                new Vector2(task.Chunk.Position.x + p2.x, task.Chunk.Position.y + p2.y),
                                Color.red);
                        }

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
            var task = new ClientCollisionTask(chunkClient);
            _clientCollisionTasks.TryAdd(chunkClient.Position, task);
        }
    }
}
