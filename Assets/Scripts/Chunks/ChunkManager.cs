using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Chunks.Tasks;
using DebugTools;
using ProceduralGeneration;
using UnityEngine;
using Utils;
using Random = System.Random;

namespace Chunks
{
    public class ChunkManager : MonoBehaviour
    {
        // PROPERTIES
        public int generatedAreaSize = 10;
        [HideInInspector] private int initialGeneratedAreaSize;
        public int cleanAreaSizeOffset = 2;
        public Transform playerTransform;

        public static Vector2 PlayerPosition;

        private GameObjectPool _chunkPool;
        private const int BatchNumber = 4;
        public readonly ChunkMap ChunkMap = new ChunkMap();
        private readonly List<ConcurrentDictionary<Vector2, SimulationTask>> _simulationBatchPool = new List<ConcurrentDictionary<Vector2, SimulationTask>>(BatchNumber);
        private readonly ChunkTaskScheduler _chunkTaskScheduler = new ChunkTaskScheduler();
        private Vector2 _playerFlooredPosition;
        private Vector2? _oldPlayerFlooredPosition;

        // DEBUG PROPERTIES
        private bool _userPressedSpace;

        private ThreadLocal<Random> Random { get; set; }

        private void Awake()
        {
            // to be able to go back to initial size if debug value is set to 0
            initialGeneratedAreaSize = generatedAreaSize;

            InitializeRandom();

            PlayerPosition = playerTransform.position;

            _chunkTaskScheduler.GetTaskManager(ChunkTaskManager.Types.Save).Processed += OnChunkSaved;
            _chunkTaskScheduler.GetTaskManager(ChunkTaskManager.Types.Load).Processed += OnChunkLoaded;
            _chunkTaskScheduler.GetTaskManager(ChunkTaskManager.Types.Generate).Processed += OnChunkGenerated;

            _chunkPool = new GameObjectPool(generatedAreaSize * generatedAreaSize);

            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
            GlobalDebugConfig.UpdateEvent += GlobalConfigUpdate;
            var restrict = GlobalDebugConfig.StaticGlobalConfig.overrideGridSize;
            if (restrict > 0)
            {
                generatedAreaSize = restrict;
            }

            for (var i = 0; i < BatchNumber; ++i)
            {
                _simulationBatchPool.Add(new ConcurrentDictionary<Vector2, SimulationTask>());
            }
        }

        private void Update()
        {
            _chunkTaskScheduler.Update();

            if (Input.GetKeyDown(KeyCode.Space))
                _userPressedSpace = true;
        }

        private void InitializeRandom()
        {
            Random = new ThreadLocal<Random>(() =>
                new Random(new Random((int) DateTimeOffset.Now.ToUnixTimeMilliseconds()).Next()));
        }

        private void ResetGrid(bool loadFromDisk)
        {
            var position = playerTransform.position;
            var flooredAroundPosition = new Vector2(Mathf.Floor(position.x), Mathf.Floor(position.y));

            _chunkTaskScheduler.CancelLoading();
            _chunkTaskScheduler.CancelGeneration();

            foreach (var chunk in ChunkMap.Map.Values)
            {
                chunk.Dispose();
            }
            ChunkMap.Clear();

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
            var restrict = GlobalDebugConfig.StaticGlobalConfig.overrideGridSize;
            var resetGrid = false;

            if (restrict == 0)
            {
                generatedAreaSize = initialGeneratedAreaSize;
                resetGrid = true;
            }
            else if (restrict > 0 && restrict != generatedAreaSize)
            {
                generatedAreaSize = restrict;
                resetGrid = true;
            }

            if (resetGrid)
                ResetGrid(true);
        }

        private void OutlineChunks()
        {
            const float s = 0.5f;
            foreach (var chunk in ChunkMap.Chunks())
            {
                var x = chunk.Position.x;
                var y = chunk.Position.y;
                var mapBorderColor = Color.white;

                // draw the map borders
                if (!ChunkMap.Contains(new Vector2(chunk.Position.x - 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), mapBorderColor);
                if (!ChunkMap.Contains(new Vector2(chunk.Position.x + 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x + s, y - s), new Vector3(x + s, y + s), mapBorderColor);
                if (!ChunkMap.Contains(new Vector2(chunk.Position.x, chunk.Position.y - 1)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), mapBorderColor);
                if (!ChunkMap.Contains(new Vector2(chunk.Position.x, chunk.Position.y + 1)))
                    Debug.DrawLine(new Vector3(x - s, y + s), new Vector3(x + s, y + s), mapBorderColor);

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
            foreach (var chunk in ChunkMap.Chunks())
            {
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

                var rx = x - 0.5f + chunk.DirtyRect.x / Chunk.Size;
                var ry = y - 0.5f + chunk.DirtyRect.y / Chunk.Size;
                var rxMax = x - 0.5f + (chunk.DirtyRect.xMax + 1) / Chunk.Size;
                var ryMax = y - 0.5f + (chunk.DirtyRect.yMax + 1) / Chunk.Size;
                Debug.DrawLine(new Vector3(rx, ry), new Vector3(rxMax, ry), dirtyRectColor);
                Debug.DrawLine(new Vector3(rx, ry), new Vector3(rx, ryMax), dirtyRectColor);
                Debug.DrawLine(new Vector3(rxMax, ry), new Vector3(rxMax, ryMax), dirtyRectColor);
                Debug.DrawLine(new Vector3(rx, ryMax), new Vector3(rxMax, ryMax), dirtyRectColor);
            }
        }

        private void FixedUpdate()
        {
            if (ShouldGenerate())
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

            if (GlobalDebugConfig.StaticGlobalConfig.drawDirtyRects)
                DrawDirtyRects();
        }

        private bool ShouldGenerate()
        {
            var position = playerTransform.position;
            _playerFlooredPosition = new Vector2(Mathf.Floor(position.x), Mathf.Floor(position.y));
            if (_oldPlayerFlooredPosition == _playerFlooredPosition)
                return false;
            _oldPlayerFlooredPosition = _playerFlooredPosition;
            return true;
        }

        private void Generate(Vector2 aroundPosition, bool loadFromDisk)
        {
            for (var x = 0; x < generatedAreaSize; ++x)
            {
                for (var y = 0; y < generatedAreaSize; ++y)
                {
                    var pos = new Vector2(aroundPosition.x + (x - generatedAreaSize / 2), aroundPosition.y + (y - generatedAreaSize / 2));
                    if (ChunkMap.Contains(pos))
                        continue;
                    _chunkTaskScheduler.QueueForGeneration(pos, loadFromDisk);
                }
            }
        }

        private void OnChunkSaved(object sender, EventArgs e)
        {
            var chunk = ((ChunkTaskManager.ChunkEventArgs) e).Chunk;
            ChunkMap.Remove(chunk.Position);
            chunk.Dispose();
            UpdateSimulationPool(chunk, false);
        }

        private void OnChunkLoaded(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskManager.ChunkEventArgs) e).Chunk);
        }

        private void OnChunkGenerated(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskManager.ChunkEventArgs) e).Chunk);
        }

        private void FinalizeChunkCreation(Chunk chunk)
        {
            ChunkMap.Add(chunk);
            chunk.GameObject = _chunkPool.GetObject();
            chunk.Texture = chunk.GameObject.GetComponent<SpriteRenderer>().sprite.texture;
            chunk.GameObject.transform.position = new Vector3(chunk.Position.x, chunk.Position.y, 0);
            chunk.GameObject.SetActive(true);
            chunk.UpdateTexture();

            UpdateSimulationPool(chunk, true);
        }

        private void Clean(Vector2 aroundPosition)
        {
            var px = aroundPosition.x - (float)generatedAreaSize / 2;
            var py = aroundPosition.y - (float)generatedAreaSize / 2;

            var chunksToRemove = new List<Vector2>();
            foreach (var chunk in ChunkMap.Chunks())
            {
                if (!(chunk.Position.x < px - cleanAreaSizeOffset) &&
                    !(chunk.Position.x > px + generatedAreaSize + cleanAreaSizeOffset) &&
                    !(chunk.Position.y < py - cleanAreaSizeOffset) &&
                    !(chunk.Position.y > py + generatedAreaSize + cleanAreaSizeOffset)) continue;
                chunksToRemove.Add(chunk.Position);
            }

            foreach (var chunkPosition in chunksToRemove)
            {
                var chunk = ChunkMap[chunkPosition];
                ChunkMap.Remove(chunkPosition);
                DisposeAndSaveChunk(chunk);
            }
        }

        private void DisposeAndSaveChunk(Chunk chunk)
        {
            if (GlobalDebugConfig.StaticGlobalConfig.disableSave)
            {
                ChunkMap.Remove(chunk.Position);
                chunk.Dispose();
                UpdateSimulationPool(chunk, false);
                return;
            }
            _chunkTaskScheduler.QueueForSaving(chunk);
        }

        private void UpdateSimulationPool(Chunk chunk, bool add)
        {
            var chunkPos = chunk.Position;
            var batchIndex = GetChunkBatchIndex(chunkPos);
            if (add)
            {
                if (_simulationBatchPool[batchIndex].ContainsKey(chunkPos))
                    return; // this chunk simulation task already exists
                var task = new SimulationTask(chunk)
                {
                    Chunks = new ChunkNeighborhood(ChunkMap, chunk, Random.Value),
                    Random = Random
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

        private void UpdateNeighborhoodsInNeighborChunks(Chunk chunk)
        {
            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    if (y == 0 && x == 0)
                        continue;
                    var neighbor = ChunkHelpers.GetNeighborChunk(ChunkMap, chunk, x, y);
                    if (neighbor != null)
                    {
                        // find the neighbor in the batch pool
                        var neighborBatchIndex = GetChunkBatchIndex(neighbor.Position);
                        if (!_simulationBatchPool[neighborBatchIndex].ContainsKey(neighbor.Position))
                        {
                            Debug.Log("UpdateNeighborhoodsInNeighborChunks: Should not happen?");
                            throw new Exception("break here, not normal");
                        }

                        var neighborTask = _simulationBatchPool[neighborBatchIndex][neighbor.Position];

                        // TODO: ideally we should only update the correct neighbor, but I'm being lazy here
                        // and its not the worst strain on performance
                        neighborTask.Chunks.UpdateNeighbors(ChunkMap, neighbor);
                    }
                }
            }
        }

        private int GetChunkBatchIndex(Vector2 position)
        {
            // small bitwise trick to find the batch index and avoid an ugly forest
            return (((int) Math.Abs(position.x) % 2) << 1)
                   | ((int) Math.Abs(position.y) % 2);
        }

        private void Simulate()
        {
            var enableDirty = !GlobalDebugConfig.StaticGlobalConfig.disableDirtySystem;
            var synchronous = GlobalDebugConfig.StaticGlobalConfig.monothreadSimulate;

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
                    if (enableDirty && !task.Chunk.Dirty)
                        continue;
                    task.Join();
                    task.Chunk.UpdateTexture();
                }
            }
        }

        private void OnDestroy()
        {
            _chunkTaskScheduler.CancelLoading();
            _chunkTaskScheduler.CancelGeneration();

            foreach (var chunk in ChunkMap.Chunks())
            {
                DisposeAndSaveChunk(chunk);
            }

            _chunkTaskScheduler.ForceSaving();

            ChunkMap.Clear();
        }
    }
}
