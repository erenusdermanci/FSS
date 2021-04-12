using System.Collections.Generic;
using Client.Player;
using Entities;
using Tools;
using UnityEngine;
using Utils;
using WorldManager = Tiles.WorldManager;

namespace Chunks.Client
{
    public class ClientCollisionManager
    {
        private readonly Dictionary<Vector2i, ClientCollisionTask> _clientCollisionTasks = new Dictionary<Vector2i, ClientCollisionTask>();

        private readonly WorldManager _worldManager;
        private readonly ChunkLayer _chunkLayer;
        private readonly PlayerInput _player;

        public ClientCollisionManager(WorldManager worldManager)
        {
            _worldManager = worldManager;
            _chunkLayer = worldManager.ChunkLayers[(int) ChunkLayerType.Foreground];
            var playerObject = GameObject.Find("Player");
            if (playerObject != null)
                _player = playerObject.GetComponent<PlayerInput>();
        }

        public void Update()
        {
            if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
                return;
            if (GlobalConfig.StaticGlobalConfig.disableCollisions)
                return;

            QueueChunkColliderGenerations();

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

        private IEnumerable<Collidable> Collidables()
        {
            if (_player != null)
                yield return _player;
            foreach (var entity in _worldManager.EntityManager.Entities.Values)
                yield return entity;
        }

        private void QueueChunkColliderGenerations()
        {
            foreach (var collidable in Collidables())
            {
                if (_chunkLayer.ClientChunkMap[collidable.ChunkPosition] == null)
                    return;

                for (var chunkIdx = 0; chunkIdx < 9; ++chunkIdx)
                {
                    if (!collidable.OverlapChunk(collidable.chunkNeighborhoodPositions[chunkIdx]))
                        continue;

                    var chunk = _chunkLayer.ClientChunkMap[collidable.chunkNeighborhoodPositions[chunkIdx]];

                    if (chunk == null)
                        continue;

                    if (!GlobalConfig.StaticGlobalConfig.disableDirtyChunks
                        && !chunk.Dirty
                        && collidable.neighborChunkColliderGenerated[chunkIdx]
                        && chunk.Collider.enabled)
                        continue;

                    if (_clientCollisionTasks.ContainsKey(chunk.Position))
                        continue;

                    var task = new ClientCollisionTask(chunk);
                    _clientCollisionTasks.Add(chunk.Position, task);
                    collidable.neighborChunkColliderGenerated[chunkIdx] = true;
                }
            }
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
