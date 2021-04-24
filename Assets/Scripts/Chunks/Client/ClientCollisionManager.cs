using System.Collections.Generic;
using Chunks.Server;
using Client.Player;
using Entities;
using Tiles;
using Tools;
using UnityEngine;
using Utils;

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

                var clientChunk = _chunkLayer.ClientChunkMap[task.Chunk.Position];
                if (clientChunk == null)
                    continue;

                var chunkCollider = clientChunk.Collider;

                chunkCollider.enabled = false;
                chunkCollider.pathCount = task.CollisionData.Count;

                for (var i = 0; i < task.CollisionData.Count; ++i)
                {
                    var coll = task.CollisionData[i];
                    for (var j = 0; j < coll.Count; ++j)
                    {
                        var tmp = coll[j];
                        tmp.Set(coll[j].x / Chunk.Size - 0.5f, coll[j].y / Chunk.Size - 0.5f);
                        coll[j] = tmp;
                    }

                    chunkCollider.SetPath(i, task.CollisionData[i]);
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
                if (_chunkLayer.ServerChunkMap[collidable.ChunkPosition] == null)
                    return;

                for (var chunkIdx = 0; chunkIdx < 9; ++chunkIdx)
                {
                    if (!collidable.OverlapChunk(collidable.chunkNeighborhoodPositions[chunkIdx]))
                        continue;

                    var clientChunk = _chunkLayer.ClientChunkMap[collidable.chunkNeighborhoodPositions[chunkIdx]];
                    var serverChunk = _chunkLayer.ServerChunkMap[collidable.chunkNeighborhoodPositions[chunkIdx]];

                    if (clientChunk == null || serverChunk == null)
                        continue;

                    if (!GlobalConfig.StaticGlobalConfig.disableDirtyChunks
                        && !serverChunk.Dirty
                        && collidable.neighborChunkColliderGenerated[chunkIdx]
                        && clientChunk.Collider.enabled)
                        continue;

                    if (_clientCollisionTasks.ContainsKey(serverChunk.Position))
                        continue;

                    var task = new ClientCollisionTask(serverChunk);
                    _clientCollisionTasks.Add(serverChunk.Position, task);
                    collidable.neighborChunkColliderGenerated[chunkIdx] = true;
                }
            }
        }

        public void QueueChunkCollisionGeneration(ChunkServer chunkServer)
        {
            if (_clientCollisionTasks.ContainsKey(chunkServer.Position))
                return;
            var task = new ClientCollisionTask(chunkServer);
            _clientCollisionTasks.Add(chunkServer.Position, task);
        }
    }
}
