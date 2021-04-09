using System.Collections.Generic;
using Tools;
using UnityEngine;
using Utils;

namespace Chunks.Client
{
    public class ClientCollisionManager
    {
        public readonly List<UpdatedGameObject> gameObjectsToUpdate = new List<UpdatedGameObject>();

        private readonly Dictionary<Vector2i, ClientCollisionTask> _clientCollisionTasks = new Dictionary<Vector2i, ClientCollisionTask>();

        private ChunkNeighborhood<ChunkClient> _chunkNeighborhood;

        private readonly ChunkLayer _chunkLayer;

        public ClientCollisionManager(ChunkLayer chunkLayer)
        {
            _chunkLayer = chunkLayer;
        }

        public void Update()
        {
            if (GlobalConfig.StaticGlobalConfig.disableCollisions)
                return;

            QueueCollisionsGenerationForUpdatedGameObjects();

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
                        vec2S[j] = new Vector2(
                            coll[j].x / (float) Chunk.Size - 0.5f,
                            coll[j].y / (float) Chunk.Size - 0.5f
                        );
                    }

                    chunkCollider.SetPath(i, vec2S);
                }

                chunkCollider.enabled = true;
            }

            _clientCollisionTasks.Clear();
        }

        private void QueueCollisionsGenerationForUpdatedGameObjects()
        {
            foreach (var updatedGameObject in gameObjectsToUpdate)
            {
                var moved = updatedGameObject.UpdateGameObjectChunkPosition();
                if (_chunkLayer.ClientChunkMap[updatedGameObject.GameObjectChunkPosition] == null)
                    return;

                if (_chunkNeighborhood == null || moved)
                {
                    _chunkNeighborhood = new ChunkNeighborhood<ChunkClient>(_chunkLayer.ClientChunkMap,
                        _chunkLayer.ClientChunkMap[updatedGameObject.GameObjectChunkPosition]);
                }

                var bounds = updatedGameObject.gameObjectCollider.bounds;
                var objectX1 = bounds.min.x - updatedGameObject.GameObjectBoundsSizeMultiplier * bounds.size.x;
                var objectX2 = bounds.max.x + updatedGameObject.GameObjectBoundsSizeMultiplier * bounds.size.x;
                var objectY1 = bounds.min.y - updatedGameObject.GameObjectBoundsSizeMultiplier * bounds.size.y;
                var objectY2 = bounds.max.y + updatedGameObject.GameObjectBoundsSizeMultiplier * bounds.size.y;

                var chunks = _chunkNeighborhood.GetChunks();
                foreach (var chunk in chunks)
                {
                    if (chunk == null)
                        continue;

                    if (!GlobalConfig.StaticGlobalConfig.disableDirtyChunks
                        && !chunk.Dirty
                        && chunk.Collider.enabled)
                        continue;

                    if (_clientCollisionTasks.ContainsKey(chunk.Position))
                        continue;

                    if (!PlayerOverlapsInChunk(chunk.Position, objectX1, objectX2, objectY1, objectY2))
                        continue;

                    var task = new ClientCollisionTask(chunk);
                    _clientCollisionTasks.Add(chunk.Position, task);
                }
            }
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
            var task = new ClientCollisionTask(chunkClient);
            _clientCollisionTasks.Add(chunkClient.Position, task);
        }
    }
}
