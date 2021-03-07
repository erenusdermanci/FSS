using System;
using System.Collections.Generic;
using System.Threading;
using ChunkTasks;
using DataComponents;
using UnityEngine;
using Utils;

namespace MonoBehaviours
{
    public class ChunkManager : MonoBehaviour
    {
        // PROPERTIES
        public int GeneratedAreaSize = 10;
        public int CleanAreaSizeOffset = 2;
        public GameObject ParentChunkObject;
        public Transform PlayerTransform;

        [Serializable]
        public struct BlockCount
        {
            public string type;
            public int count;
        }

        public BlockCount[] blockCountsAtGenerate;
        public BlockCount[] blockCounts;

        public ChunkGrid ChunkGrid;
        private const int BatchNumber = 4;
        private readonly List<List<SimulationTask>> _simulationBatchPool = new List<List<SimulationTask>>(BatchNumber);
        private readonly List<ChunkTask> _generationTasks = new List<ChunkTask>();
        private Vector2 _playerFlooredPosition;
        private Vector2? _oldPlayerFlooredPosition = null;

        // DEBUG PROPERTIES
        private bool UserPressedSpace = false;

        private ThreadLocal<System.Random> Random { get; set; }
        private ObjectPool _chunkPool;

        private void Awake()
        {
            InitializeRandom();
            _chunkPool = new ObjectPool(GeneratedAreaSize * GeneratedAreaSize);
            var blockNames = Enum.GetNames(typeof(BlockConstants.Blocks));
            blockCountsAtGenerate = new BlockCount[blockNames.Length];
            blockCounts = new BlockCount[blockNames.Length];
            for (var i = 0; i < blockCountsAtGenerate.Length; ++i)
            {
                blockCountsAtGenerate[i].type = blockNames[i];
                blockCounts[i].type = blockNames[i];
            }
            ChunkGrid = new ChunkGrid();
            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
            GlobalDebugConfig.UpdateEvent += GlobalConfigUpdate;
            var restrict = GlobalDebugConfig.StaticGlobalConfig.RestrictGridSize;
            if (restrict > 0)
            {
                GeneratedAreaSize = restrict;
            }

            for (var i = 0; i < BatchNumber; ++i)
            {
                _simulationBatchPool.Add(new List<SimulationTask>());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                UserPressedSpace = true;
        }

        private void InitializeRandom()
        {
            Random = new ThreadLocal<System.Random>(() =>
                new System.Random(new System.Random((int) DateTimeOffset.Now.ToUnixTimeMilliseconds()).Next()));
        }

        private void ResetGrid(bool loadFromDisk)
        {
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            ChunkGrid.Dispose();
            ChunkGrid = new ChunkGrid();
            Generate(flooredAroundPosition, loadFromDisk);
            UpdateSimulationBatches();
        }

        private void ProceduralGeneratorUpdate(object sender, EventArgs e)
        {
            ResetGrid(false);
        }
    
        private void GlobalConfigUpdate(object sender, EventArgs e)
        {
            var restrict = GlobalDebugConfig.StaticGlobalConfig.RestrictGridSize;
            if (restrict > 0 && restrict != GeneratedAreaSize || !GlobalDebugConfig.StaticGlobalConfig.EnableSimulation)
            {
                GeneratedAreaSize = restrict;
                ResetGrid(true);
            }
        }

        private void OutlineChunks()
        {
            const float s = 0.4975f;
            foreach (var chunk in ChunkGrid.ChunkMap.Values)
            {
                var borderColor = chunk.Dirty ? Color.red : Color.white;
                var x = chunk.Position.x;
                var y = chunk.Position.y;
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), borderColor);
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x + s, y - s), borderColor);
            }
        }

        private void FixedUpdate()
        {
            if (ShouldGenerate())
            {
                Generate(_playerFlooredPosition, true);
                Clean(_playerFlooredPosition);
                UpdateSimulationBatches();
            }

            if (GlobalDebugConfig.StaticGlobalConfig.EnableSimulation)
            {
                if (GlobalDebugConfig.StaticGlobalConfig.StepByStep && UserPressedSpace)
                {
                    UserPressedSpace = false;
                    Simulate();
                }
                else if (!GlobalDebugConfig.StaticGlobalConfig.PauseSimulation)
                    Simulate();
            }
            if (GlobalDebugConfig.StaticGlobalConfig.OutlineChunks)
            {
                OutlineChunks();
            }
        }

        private bool ShouldGenerate()
        {
            var position = PlayerTransform.position;
            _playerFlooredPosition = new Vector2(Mathf.Floor(position.x), Mathf.Floor(position.y));
            if (_oldPlayerFlooredPosition != null && _oldPlayerFlooredPosition == _playerFlooredPosition)
                return false;
            _oldPlayerFlooredPosition = _playerFlooredPosition;
            return true;
        }

        private void Generate(Vector2 aroundPosition, bool loadFromDisk)
        {
            _generationTasks.Clear();

            for (var x = 0; x < GeneratedAreaSize; ++x)
            {
                for (var y = 0; y < GeneratedAreaSize; ++y)
                {
                    var pos = new Vector2(aroundPosition.x + (x - GeneratedAreaSize / 2), aroundPosition.y + (y - GeneratedAreaSize / 2));
                    if (ChunkGrid.ChunkMap.ContainsKey(pos))
                        continue;

                    var generated = false;
                    Chunk chunk = null;
                    if (!GlobalDebugConfig.StaticGlobalConfig.DisablePersistence
                        && loadFromDisk)
                    {
                        chunk = ChunkHelpers.Load(pos);
                    }
                    if (chunk == null)
                    {
                        chunk = new Chunk { Position = pos };
                        _generationTasks.Add(new GenerationTask(chunk)
                        {
                            Rng = Random
                        });
                        generated = true;
                    }
                    ChunkGrid.ChunkMap.Add(pos, chunk);

                    chunk.GameObject = _chunkPool.GetObject();
                    chunk.Texture = chunk.GameObject.GetComponent<SpriteRenderer>().sprite.texture;
                    chunk.GameObject.transform.position = new Vector3(chunk.Position.x, chunk.Position.y, 0);
                    chunk.GameObject.transform.parent = ParentChunkObject.transform;
                    chunk.GameObject.SetActive(true);
                    
                    if (!generated)
                        chunk.UpdateTexture();
                }
            }

            foreach (var task in _generationTasks)
            {
                task.Schedule();
            }

            foreach (var task in _generationTasks)
            {
                task.Join();
                task.Chunk.UpdateTexture();
            }

            for (var i = 0; i < blockCountsAtGenerate.Length; ++i)
                blockCountsAtGenerate[i].count = 0;
            foreach (var chunk in ChunkGrid.ChunkMap.Values)
            {
                for (var i = 0; i < blockCountsAtGenerate.Length; ++i)
                    blockCountsAtGenerate[i].count += chunk.BlockCounts[i];
            }
        }

        private void Clean(Vector2 aroundPosition)
        {
            var px = aroundPosition.x - GeneratedAreaSize / 2;
            var py = aroundPosition.y - GeneratedAreaSize / 2;

            var chunksToRemove = new List<Vector2>();
            foreach (var chunk in ChunkGrid.ChunkMap.Values)
            {
                if (!(chunk.Position.x < px - CleanAreaSizeOffset) &&
                    !(chunk.Position.x > px + GeneratedAreaSize + CleanAreaSizeOffset) &&
                    !(chunk.Position.y < py - CleanAreaSizeOffset) &&
                    !(chunk.Position.y > py + GeneratedAreaSize + CleanAreaSizeOffset)) continue;
                chunksToRemove.Add(chunk.Position);
                chunk.Dispose();
            }

            foreach (var chunkPosition in chunksToRemove)
            {
                ChunkGrid.ChunkMap.Remove(chunkPosition);
            }
        }

        private void UpdateSimulationBatches()
        {
            for (var i = 0; i < BatchNumber; ++i)
            {
                _simulationBatchPool[i].Clear();
            }
            foreach (var chunk in ChunkGrid.ChunkMap.Values)
            {
                var chunkPos = chunk.Position;
                // small bitwise trick to find the batch index and avoid an ugly forest
                var batchIndex = (((int) Math.Abs(chunkPos.x) % 2) << 1)
                                 | ((int) Math.Abs(chunkPos.y) % 2);
                _simulationBatchPool[batchIndex].Add(new SimulationTask(chunk)
                {
                    Chunks = new ChunkNeighborhood(ChunkGrid, chunk),
                    Random = Random
                });
            }
        }

        private void Simulate()
        {
            var enableDirty = !GlobalDebugConfig.StaticGlobalConfig.DisableDirtySystem;
            var synchronous = GlobalDebugConfig.StaticGlobalConfig.MonothreadSimulate;
            for (var i = 0; i < blockCounts.Length; ++i)
                blockCounts[i].count = 0;
            foreach (var batch in _simulationBatchPool)
            {
                foreach (var task in batch)
                {
                    if (enableDirty && !task.Chunk.Dirty)
                        continue;
                    task.Schedule(synchronous);
                }

                foreach (var task in batch)
                {
                    if (enableDirty && !task.Chunk.Dirty)
                        continue;
                    task.Join();
                    task.Chunk.UpdateTexture();
                }
            }

            foreach (var chunk in ChunkGrid.ChunkMap.Values)
            {
                for (var i = 0; i < blockCounts.Length; ++i)
                    blockCounts[i].count += chunk.BlockCounts[i];
            }
        }

        private void OnDestroy()
        {
            ChunkGrid.Dispose();
        }
    }
}
