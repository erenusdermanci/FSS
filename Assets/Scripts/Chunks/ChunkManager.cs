using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Chunks.Tasks;
using Collision;
using DebugTools;
using ProceduralGeneration;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

namespace Chunks
{
    public class ChunkManager : MonoBehaviour
    {
        // PROPERTIES
        public int generatedAreaSize = 10;
        private int _initialGeneratedAreaSize;
        public int cleanAreaSizeOffset = 2;
        public Transform playerTransform;

        public static Vector2 PlayerPosition;

        private GameObjectPool _chunkPool;
        private const int BatchNumber = 4;
        public readonly ChunkMap<ChunkServer> ServerChunkMap = new ChunkMap<ChunkServer>();
        public readonly ChunkMap<ChunkClient> ClientChunkMap = new ChunkMap<ChunkClient>();
        private readonly List<ConcurrentDictionary<Vector2i, SimulationTask>> _simulationBatchPool = new List<ConcurrentDictionary<Vector2i, SimulationTask>>(BatchNumber);
        private readonly ChunkTaskScheduler _chunkTaskScheduler = new ChunkTaskScheduler();
        private Vector2i _playerFlooredPosition;
        private Vector2i? _oldPlayerFlooredPosition;
        private bool _playerHasMoved;
        private ChunkNeighborhood<ChunkClient> _playerChunkNeighborhood;

        public static int UpdatedFlag;

        // DEBUG PROPERTIES
        private bool _userPressedSpace;

        private void Awake()
        {
            // to be able to go back to initial size if debug value is set to 0
            _initialGeneratedAreaSize = generatedAreaSize;

            PlayerPosition = playerTransform.position;

            _chunkTaskScheduler.GetTaskManager(ChunkTaskTypes.Save).Processed += OnChunkSaved;
            _chunkTaskScheduler.GetTaskManager(ChunkTaskTypes.Load).Processed += OnChunkLoaded;
            _chunkTaskScheduler.GetTaskManager(ChunkTaskTypes.Generate).Processed += OnChunkGenerated;

            _chunkPool = new GameObjectPool(generatedAreaSize * generatedAreaSize);

            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
            GlobalDebugConfig.UpdateEvent += GlobalConfigUpdate;
            GlobalDebugConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;
            var restrict = GlobalDebugConfig.StaticGlobalConfig.overrideGridSize;
            if (restrict > 0)
            {
                generatedAreaSize = restrict;
            }

            for (var i = 0; i < BatchNumber; ++i)
            {
                _simulationBatchPool.Add(new ConcurrentDictionary<Vector2i, SimulationTask>());
            }

            UpdatedFlag = 1;
        }

        private void Update()
        {
            _chunkTaskScheduler.Update();

            if (Input.GetKeyDown(KeyCode.Space))
                _userPressedSpace = true;
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        private void ResetGrid(bool loadFromDisk)
        {
            var position = playerTransform.position;
            var flooredAroundPosition = new Vector2i((int) Mathf.Floor(position.x), (int) Mathf.Floor(position.y));

            _chunkTaskScheduler.CancelLoading();
            _chunkTaskScheduler.CancelGeneration();

            foreach (var clientChunk in ClientChunkMap.Map.Values)
            {
                clientChunk.Dispose();
            }
            ClientChunkMap.Clear();

            foreach (var serverChunk in ServerChunkMap.Map.Values)
            {
                serverChunk.Dispose();
            }
            ServerChunkMap.Clear();

            foreach (var batch in _simulationBatchPool)
            {
                batch.Clear();
            }

            Generate(flooredAroundPosition, loadFromDisk);
        }

        private void ProceduralGeneratorUpdate(object sender, EventArgs e)
        {
            ResetGrid(false);
        }

        private void GlobalConfigUpdate(object sender, EventArgs e)
        {
            var overrideGridSize = GlobalDebugConfig.StaticGlobalConfig.overrideGridSize;
            var resetGrid = false;

            if (overrideGridSize < 0) // invalid value
                return;

            if (overrideGridSize == 0) // disable override
            {
                if (generatedAreaSize == _initialGeneratedAreaSize) // already at default value
                    return;

                generatedAreaSize = _initialGeneratedAreaSize;
                resetGrid = true;
            }
            else if (generatedAreaSize != overrideGridSize)
            {
                generatedAreaSize = overrideGridSize;
                resetGrid = true;
            }

            if (resetGrid)
                ResetGrid(true);
        }

        private void OutlineChunks()
        {
            const float s = 0.5f;
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                var x = chunk.Position.x;
                var y = chunk.Position.y;
                var mapBorderColor = Color.white;

                // draw the map borders
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x - 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x + 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x + s, y - s), new Vector3(x + s, y + s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y - 1)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y + 1)))
                    Debug.DrawLine(new Vector3(x - s, y + s), new Vector3(x + s, y + s), mapBorderColor);

                if (GlobalDebugConfig.StaticGlobalConfig.hideCleanChunkOutlines && !chunk.Dirty)
                    continue;

                // draw the chunk borders
                var borderColor = chunk.Dirty ? Color.red : Color.white;
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), borderColor);
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x + s, y - s), borderColor);
            }
        }

        private void DrawDirtyRects()
        {
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                if (!chunk.Dirty)
                    continue;

                var x = chunk.Position.x;
                var y = chunk.Position.y;

                var chunkBatchIndex = GetChunkBatchIndex(chunk.Position);
                Color32 dirtyRectColor;
                switch (chunkBatchIndex)
                {
                    case 0:
                        dirtyRectColor = Color.green;
                        break;
                    case 1:
                        dirtyRectColor = Color.red;
                        break;
                    case 2:
                        dirtyRectColor = Color.magenta;
                        break;
                    case 3:
                        dirtyRectColor = Color.yellow;
                        break;
                    default:
                        return;
                }
                for (var i = 0; i < chunk.DirtyRects.Length; ++i)
                {
                    if (chunk.DirtyRects[i].X < 0.0f)
                        continue;
                    var rx = x - 0.5f + (chunk.DirtyRects[i].X + ChunkServer.DirtyRectX[i]) / (float)Chunk.Size;
                    var rxMax = x - 0.5f + (chunk.DirtyRects[i].XMax + ChunkServer.DirtyRectX[i] + 1) / (float)Chunk.Size;
                    var ry = y - 0.5f + (chunk.DirtyRects[i].Y + ChunkServer.DirtyRectY[i]) / (float)Chunk.Size;
                    var ryMax = y - 0.5f + (chunk.DirtyRects[i].YMax + ChunkServer.DirtyRectY[i] + 1) / (float)Chunk.Size;
                    Debug.DrawLine(new Vector3(rx, ry), new Vector3(rxMax, ry), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rx, ry), new Vector3(rx, ryMax), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rxMax, ry), new Vector3(rxMax, ryMax), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rx, ryMax), new Vector3(rxMax, ryMax), dirtyRectColor);
                }
            }
        }

        private void FixedUpdate()
        {
            _playerHasMoved = PlayerHasMoved();

            if (_playerHasMoved)
            {
                PlayerPosition = playerTransform.position;
                Generate(_playerFlooredPosition, true);
                Clean(_playerFlooredPosition);
            }

            if (GlobalDebugConfig.StaticGlobalConfig.stepByStep && _userPressedSpace)
            {
                _userPressedSpace = false;
                Simulate();
            }
            else if (!GlobalDebugConfig.StaticGlobalConfig.pauseSimulation)
                Simulate();

            if (GlobalDebugConfig.StaticGlobalConfig.outlineChunks)
                OutlineChunks();

            if (!GlobalDebugConfig.StaticGlobalConfig.disableDirtyRects && GlobalDebugConfig.StaticGlobalConfig.drawDirtyRects)
                DrawDirtyRects();

            if (!GlobalDebugConfig.StaticGlobalConfig.disableCollisions)
                GenerateCollisions();

        }

        private bool PlayerHasMoved()
        {
            var position = playerTransform.position;
            _playerFlooredPosition = new Vector2i((int) Mathf.Floor(position.x), (int) Mathf.Floor(position.y));
            if (_oldPlayerFlooredPosition == _playerFlooredPosition)
                return false;
            _oldPlayerFlooredPosition = _playerFlooredPosition;
            return true;
        }

        private void Generate(Vector2i aroundPosition, bool loadFromDisk)
        {
            for (var x = 0; x < generatedAreaSize; ++x)
            {
                for (var y = 0; y < generatedAreaSize; ++y)
                {
                    var pos = new Vector2i(aroundPosition.x + (x - generatedAreaSize / 2), aroundPosition.y + (y - generatedAreaSize / 2));
                    if (ServerChunkMap.Contains(pos))
                        continue;
                    _chunkTaskScheduler.QueueForGeneration(pos, loadFromDisk);
                }
            }
        }

        private void OnChunkSaved(object sender, EventArgs e)
        {
            var chunk = ((ChunkTaskEvent) e).Chunk;
            ServerChunkMap[chunk.Position]?.Dispose();
            ServerChunkMap.Remove(chunk.Position);
            ClientChunkMap[chunk.Position]?.Dispose();
            ClientChunkMap.Remove(chunk.Position);
            UpdateSimulationPool(chunk, false);
        }

        private void OnChunkLoaded(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskEvent) e).Chunk);
        }

        private void OnChunkGenerated(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskEvent) e).Chunk);
        }

        private void FinalizeChunkCreation(ChunkServer chunk)
        {
            ServerChunkMap.Add(chunk);

            var chunkGameObject = _chunkPool.GetObject();
            chunkGameObject.transform.position = new Vector3(chunk.Position.x, chunk.Position.y, 0);
            var clientChunk = new ChunkClient
            {
                Position = chunk.Position,
                Colors = chunk.Data.colors, // pointer on ChunkServer colors,
                Types = chunk.Data.types, // pointer on ChunkServer types,
                GameObject = chunkGameObject,
                Texture = chunkGameObject.GetComponent<SpriteRenderer>().sprite.texture
            };
            chunkGameObject.SetActive(true);
            ClientChunkMap.Add(clientChunk);
            clientChunk.UpdateTexture();

            UpdateSimulationPool(chunk, true);
        }

        public void CreateClientChunk(ChunkServer serverChunk)
        {

        }

        private void Clean(Vector2i aroundPosition)
        {
            var px = aroundPosition.x - (float)generatedAreaSize / 2;
            var py = aroundPosition.y - (float)generatedAreaSize / 2;

            var chunksToRemove = new List<Vector2i>();
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                if (!(chunk.Position.x < px - cleanAreaSizeOffset) &&
                    !(chunk.Position.x > px + generatedAreaSize + cleanAreaSizeOffset) &&
                    !(chunk.Position.y < py - cleanAreaSizeOffset) &&
                    !(chunk.Position.y > py + generatedAreaSize + cleanAreaSizeOffset)) continue;
                chunksToRemove.Add(chunk.Position);
            }

            foreach (var chunkPosition in chunksToRemove)
            {
                var chunk = ServerChunkMap[chunkPosition];
                ServerChunkMap.Remove(chunkPosition);
                DisposeAndSaveChunk(chunk);
            }
        }

        private void DisposeAndSaveChunk(ChunkServer chunk)
        {
            if (GlobalDebugConfig.StaticGlobalConfig.disableSave)
            {
                ServerChunkMap[chunk.Position]?.Dispose();
                ServerChunkMap.Remove(chunk.Position);
                ClientChunkMap[chunk.Position]?.Dispose();
                ClientChunkMap.Remove(chunk.Position);
                UpdateSimulationPool(chunk, false);
                return;
            }
            _chunkTaskScheduler.QueueForSaving(chunk);
        }

        private void UpdateSimulationPool(ChunkServer chunk, bool add)
        {
            var chunkPos = chunk.Position;
            var batchIndex = GetChunkBatchIndex(chunkPos);
            if (add)
            {
                if (_simulationBatchPool[batchIndex].ContainsKey(chunkPos))
                    return; // this chunk simulation task already exists
                var task = new SimulationTask(chunk)
                {
                    Chunks = new ChunkServerNeighborhood(ServerChunkMap, chunk),
                };
                _simulationBatchPool[batchIndex].TryAdd(chunkPos, task);
            }
            else
            {
                _simulationBatchPool[batchIndex].TryRemove(chunkPos, out _);
            }

            // update all chunks tasks that need or had me as a neighbour
            UpdateNeighborhoodsInNeighborChunks(chunk);
        }

        private void UpdateNeighborhoodsInNeighborChunks(ChunkServer chunk)
        {
            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    if (y == 0 && x == 0)
                        continue;
                    var neighbor = ChunkHelpers.GetNeighborChunk(ServerChunkMap, chunk, x, y);
                    if (neighbor != null)
                    {
                        // find the neighbor in the batch pool
                        var neighborBatchIndex = GetChunkBatchIndex(neighbor.Position);
                        var neighborTask = _simulationBatchPool[neighborBatchIndex][neighbor.Position];

                        // TODO: ideally we should only update the correct neighbor, but I'm being lazy here
                        // and its not the worst strain on performance
                        neighborTask.Chunks.UpdateNeighbors(ServerChunkMap, neighbor);
                    }
                }
            }
        }

        private int GetChunkBatchIndex(Vector2i position)
        {
            // small bitwise trick to find the batch index and avoid an ugly forest
            return ((Math.Abs(position.x) % 2) << 1)
                   | (Math.Abs(position.y) % 2);
        }

        private void Simulate()
        {
            UpdatedFlag++;

            var enableDirty = !GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks;
            var synchronous = GlobalDebugConfig.StaticGlobalConfig.monoThreadSimulate;

            foreach (var batch in _simulationBatchPool)
            {
                foreach (var task in batch.Values)
                {
                    if (enableDirty && !task.Chunk.Dirty)
                        continue;
                    task.Schedule(synchronous);
                }

                foreach (var task in batch.Values)
                {
                    if (!task.Scheduled())
                        continue;
                    task.Join();
                }
            }

            foreach (var clientChunk in ClientChunkMap.Map.Values)
            {
                clientChunk.Dirty = ServerChunkMap[clientChunk.Position].Dirty;
                clientChunk.UpdateTexture();
            }
        }

        private void GenerateCollisions()
        {
            if (ClientChunkMap[_playerFlooredPosition] == null)
                return;

            if (_playerChunkNeighborhood == null || _playerHasMoved)
            {
                _playerChunkNeighborhood = new ChunkNeighborhood<ChunkClient>(ClientChunkMap,
                    ClientChunkMap[_playerFlooredPosition]);
            }

            foreach (var chunk in _playerChunkNeighborhood.GetChunks())
            {
                if (chunk == null
                    || !GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks && !chunk.Dirty
                    && (chunk.GameObject.GetComponent<EdgeCollider2D>() != null))
                    continue;

                foreach (var previousPoly in chunk.GameObject.GetComponents<EdgeCollider2D>())
                {
                    Destroy(previousPoly);
                }

                var collisionData = ChunkCollision.ComputeChunkColliders(chunk);

                foreach (var coll in collisionData)
                {
                    var vec2s = new Vector2[coll.Count + 1];
                    for (var i = 0; i < coll.Count; ++i)
                    {
                        var x = (float)(coll[i].x) / (float)(Chunk.Size);
                        var y = (float) (coll[i].y) / (float)(Chunk.Size);
                        x -= 0.5f;
                        y -= 0.5f;
                        vec2s[i] = new Vector2(x, y);
                    }

                    vec2s[coll.Count] = vec2s[0];

                    var attachedCollider = chunk.GameObject.AddComponent<EdgeCollider2D>();
                    attachedCollider.points = vec2s;
                }
            }
        }

        private void OnDestroy()
        {
            _chunkTaskScheduler.CancelLoading();
            _chunkTaskScheduler.CancelGeneration();

            foreach (var chunk in ServerChunkMap.Chunks())
            {
                DisposeAndSaveChunk(chunk);
            }

            _chunkTaskScheduler.ForceSaving();

            ServerChunkMap.Clear();
        }
    }
}
