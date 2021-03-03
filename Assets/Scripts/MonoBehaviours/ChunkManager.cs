using System;
using System.Collections.Generic;
using System.Threading;
using ChunkTasks;
using DataComponents;
using Unity.Collections;
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

        private ObjectPool _chunkPool;
        public ChunkGrid _chunkGrid;
        private const int BatchNumber = 4;

        // DEBUG PROPERTIES
        private bool UserPressedSpace = false;

        private NativeArray<Unity.Mathematics.Random> RandomArray { get; set; }

        private void Awake()
        {
            InitializeRandom();
            var blockNames = Enum.GetNames(typeof(Constants.Blocks));
            blockCountsAtGenerate = new BlockCount[blockNames.Length];
            blockCounts = new BlockCount[blockNames.Length];
            for (var i = 0; i < blockCountsAtGenerate.Length; ++i)
            {
                blockCountsAtGenerate[i].type = blockNames[i];
                blockCounts[i].type = blockNames[i];
            }
            _chunkPool = GetComponent<ObjectPool>();
            _chunkGrid = new ChunkGrid();
            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
            GlobalDebugConfig.UpdateEvent += GlobalConfigUpdate;
            var restrict = GlobalDebugConfig.StaticGlobalConfig.RestrictGridSize;
            if (restrict > 0)
            {
                GeneratedAreaSize = restrict;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                UserPressedSpace = true;
        }

        private void InitializeRandom()
        {
            ThreadPool.GetMaxThreads(out var workerThreads, out _);
            var randomArray = new Unity.Mathematics.Random[workerThreads];
            var seed = new System.Random();

            for (var i = 0; i < workerThreads; ++i)
                randomArray[i] = new Unity.Mathematics.Random((uint)seed.Next());

            RandomArray = new NativeArray<Unity.Mathematics.Random>(randomArray, Allocator.Persistent);
        }

        private void ResetGrid()
        {
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            _chunkGrid.Dispose();
            _chunkGrid = new ChunkGrid();
            Generate(flooredAroundPosition);
        }

        private void ProceduralGeneratorUpdate(object sender, EventArgs e)
        {
            ResetGrid();
        }

        private void GlobalConfigUpdate(object sender, EventArgs e)
        {
            var restrict = GlobalDebugConfig.StaticGlobalConfig.RestrictGridSize;
            if (restrict > 0 && restrict != GeneratedAreaSize || !GlobalDebugConfig.StaticGlobalConfig.EnableSimulation)
            {
                GeneratedAreaSize = restrict;
                ResetGrid();
            }
        }

        private void OutlineChunks()
        {
            const float s = 0.49f;
            foreach (var chunk in _chunkGrid.ChunkMap.Values)
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
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            Generate(flooredAroundPosition);
            Clean(flooredAroundPosition);
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

        private void Generate(Vector2 aroundPosition)
        {
            var generationTasks = new List<ChunkTask>();

            for (var x = 0; x < GeneratedAreaSize; ++x)
            {
                for (var y = 0; y < GeneratedAreaSize; ++y)
                {
                    var pos = new Vector2(aroundPosition.x + (x - GeneratedAreaSize / 2), aroundPosition.y + (y - GeneratedAreaSize / 2));
                    if (_chunkGrid.ChunkMap.ContainsKey(pos))
                        continue;

                    var chunk = new Chunk(pos);
                    _chunkGrid.ChunkMap.Add(pos, chunk);

                    generationTasks.Add(new GenerationTask(chunk)
                    {
                        ChunkPos = pos,
                        BlockColors = chunk.BlockColors,
                        BlockTypes = chunk.BlockTypes
                    });
                    
                    chunk.GameObject = _chunkPool.GetObject();
                    chunk.Texture = chunk.GameObject.GetComponent<SpriteRenderer>().sprite.texture;
                    chunk.GameObject.transform.position = new Vector3(pos.x, pos.y, 0);
                    chunk.GameObject.transform.parent = ParentChunkObject.transform;
                    chunk.GameObject.SetActive(true);
                }
            }

            foreach (var task in generationTasks)
            {
                task.Schedule();
            }

            foreach (var task in generationTasks)
            {
                task.Join();
                task.ReloadTexture();
                for (var i = 0; i < blockCountsAtGenerate.Length; ++i)
                    blockCountsAtGenerate[i].count += task.BlockCounts[i];
            }
        }

        private void Clean(Vector2 aroundPosition)
        {
            var px = aroundPosition.x - GeneratedAreaSize / 2;
            var py = aroundPosition.y - GeneratedAreaSize / 2;

            var chunksToRemove = new List<Vector2>();
            foreach (var chunk in _chunkGrid.ChunkMap.Values)
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
                _chunkGrid.ChunkMap.Remove(chunkPosition);
            }
        }

        private void Simulate()
        {
            var batchPool = new List<List<SimulationTask>>(BatchNumber);
            for (var i = 0; i < BatchNumber; ++i)
            {
                batchPool.Add(new List<SimulationTask>());
            }

            foreach (var chunk in _chunkGrid.ChunkMap.Values)
            {
                if (!chunk.Dirty) continue;
                var chunkPos = chunk.Position;
                // small bitwise trick to find the batch index and avoid an ugly forest
                var batchIndex = (((int) Math.Abs(chunkPos.x) % 2) << 1)
                                 | ((int) Math.Abs(chunkPos.y) % 2);
                batchPool[batchIndex].Add(new SimulationTask(chunk)
                {
                    Chunks = new ChunkNeighborhood(_chunkGrid, chunk),
                    RandomArray = RandomArray
                });
            }
            
            for (var i = 0; i < blockCounts.Length; ++i)
                blockCounts[i].count = 0;
            var synchronous = GlobalDebugConfig.StaticGlobalConfig.MonothreadSimulate;
            foreach (var batch in batchPool)
            {
                foreach (var task in batch)
                    task.Schedule(synchronous);
                foreach (var task in batch)
                {
                    task.Join();
                    task.ReloadTexture();
                    for (var i = 0; i < blockCounts.Length; ++i)
                        blockCounts[i].count += task.BlockCounts[i];
                }
            }
        }



        private void OnDestroy()
        {
            _chunkGrid.Dispose();
        }
    }
}
