using System.Collections.Concurrent;
using DebugTools;
using UnityEngine;
using Utils;

namespace Chunks.Client
{
    public class ClientCollisionManager
    {
        private readonly ConcurrentDictionary<Vector2i, ClientCollisionTask> _clientCollisionTasks
            = new ConcurrentDictionary<Vector2i, ClientCollisionTask>();

        private ChunkNeighborhood<ChunkClient> _playerChunkNeighborhood;
        private bool[] _playerChunkNeighborhoodCleanFlags;

        private readonly ChunkLayer _chunkLayer;

        private const float PlayerBoxSizeMultiplier = 0.5f;

        private readonly CapsuleCollider2D playerCapsuleCollider;

        public ClientCollisionManager(ChunkLayer chunkLayer)
        {
            _chunkLayer = chunkLayer;

            playerCapsuleCollider = GameObject.Find("Player").GetComponent<CapsuleCollider2D>();
        }

        public void GenerateCollisions(Vector2i playerFlooredPosition, bool playerHasMovedChunks)
        {
            if (_chunkLayer.ClientChunkMap[playerFlooredPosition] == null)
                return;

            if (_playerChunkNeighborhood == null || playerHasMovedChunks)
            {
                _playerChunkNeighborhood = new ChunkNeighborhood<ChunkClient>(_chunkLayer.ClientChunkMap,
                    _chunkLayer.ClientChunkMap[playerFlooredPosition]);
                _playerChunkNeighborhoodCleanFlags = new bool[9];
            }

            var capsuleBounds = playerCapsuleCollider.bounds;
            // box around player
            var playerX1 = capsuleBounds.min.x - (PlayerBoxSizeMultiplier * capsuleBounds.size.x);
            var playerX2 = capsuleBounds.max.x + (PlayerBoxSizeMultiplier * capsuleBounds.size.x);
            var playerY1 = capsuleBounds.min.y - (PlayerBoxSizeMultiplier * capsuleBounds.size.y);
            var playerY2 = capsuleBounds.max.y + (PlayerBoxSizeMultiplier * capsuleBounds.size.y);

            var chunks = _playerChunkNeighborhood.GetChunks();
            for (var chunkIdx = 0; chunkIdx < chunks.Length; ++chunkIdx)
            {
                var chunk = chunks[chunkIdx];

                if (chunk == null)
                    continue;

                if (!GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks
                    && !chunk.Dirty
                    && _playerChunkNeighborhoodCleanFlags[chunkIdx]
                    && chunk.Collider.enabled)
                    continue;

                if (!PlayerOverlapsInChunk(chunk.Position, playerX1, playerX2, playerY1, playerY2))
                    continue;

                var task = new ClientCollisionTask(chunk, _chunkLayer.type);
                _clientCollisionTasks.TryAdd(chunk.Position, task);
                _playerChunkNeighborhoodCleanFlags[chunkIdx] = true;
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

        private static bool PlayerOverlapsInChunk(Vector2i chunkPos,
            float playerX1, float playerX2,
            float playerY1, float playerY2)
        {
            var chunkX1 = chunkPos.x - 0.5f;
            var chunkX2 = chunkX1 + 1.0f;
            var chunkY1 = chunkPos.y - 0.5f;
            var chunkY2 = chunkY1 + 1.0f;

            return playerX2 >= chunkX1 && chunkX2 >= playerX1 && playerY2 >= chunkY1 && chunkY2 >= playerY1;
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
