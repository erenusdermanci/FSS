using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChunkTasks;
using DataComponents;
using Unity.Collections;
using UnityEngine;

namespace MonoBehaviours
{
    public class ChunkManager : MonoBehaviour
    {
        // PROPERTIES
        public int GeneratedAreaSize = 10;
        public int CleanAreaSizeOffset = 2;
        public GameObject ChunkPrefab;
        public GameObject ParentChunkObject;
        public Transform PlayerTransform;

        private ChunkGrid _chunkGrid;
        private const int BatchNumber = 4;

        // DEBUG PROPERTIES
        public GameObject DebugBorderPrefab;
        private bool UserPressedSpace = false;

        private NativeArray<Unity.Mathematics.Random> RandomArray { get; set; }

        private void Awake()
        {
            InitializeRandom();
            _chunkGrid = new ChunkGrid();
            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
            GlobalConfig.UpdateEvent += GlobalConfigUpdate;
            var restrict = GlobalConfig.StaticGlobalConfig.RestrictGridSize;
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
            var restrict = GlobalConfig.StaticGlobalConfig.RestrictGridSize;
            if (restrict > 0 && restrict != GeneratedAreaSize || !GlobalConfig.StaticGlobalConfig.EnableSimulation)
            {
                GeneratedAreaSize = restrict;
                ResetGrid();
            }

            if (!GlobalConfig.StaticGlobalConfig.OutlineChunks)
            {
                ClearOutline(); // yes this will run through the chunkmap, but only when globalconfig is updated
                // so its "fine"
            }
        }

        private void ClearOutline()
        {
            // Clean last frame
            foreach (var chunk in _chunkGrid.ChunkMap)
            {
                if (chunk.Value.GameObject.transform.childCount > 0)
                {
                    for (int i = 0; i < chunk.Value.GameObject.transform.childCount; i++)
                    {
                        var existingChild = chunk.Value.GameObject.transform.GetChild(i)?.gameObject;
                        if (existingChild != null)
                        {
                            existingChild.SetActive(false);
                            Destroy(existingChild);
                        }
                    }
                }
            }
        }

        private void OutlineChunks()
        {
            ClearOutline();

            // Draw this frame
            foreach (var chunk in _chunkGrid.ChunkMap)
            {
                var pos = chunk.Key;
                var borderChunk = Instantiate(DebugBorderPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                borderChunk.transform.parent = chunk.Value.GameObject.transform;
                borderChunk.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 63); // transparent white
                borderChunk.SetActive(true);
            }
        }

        private void FixedUpdate()
        {
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            Generate(flooredAroundPosition);
            Clean(flooredAroundPosition);
            if (GlobalConfig.StaticGlobalConfig.EnableSimulation)
            {
                if (GlobalConfig.StaticGlobalConfig.StepByStep && UserPressedSpace)
                {
                    UserPressedSpace = false;
                    Simulate();
                }
                else if (!GlobalConfig.StaticGlobalConfig.PauseSimulation)
                    Simulate();
            }
            if (GlobalConfig.StaticGlobalConfig.OutlineChunks)
            {
                OutlineChunks();
            }
        }

        private void Generate(Vector2 aroundPosition)
        {
            var generateJobs = new List<GenerationTask>();
            var generateTasks = new List<Task>();

            for (var x = 0; x < GeneratedAreaSize; ++x)
            {
                for (var y = 0; y < GeneratedAreaSize; ++y)
                {
                    var pos = new Vector2(aroundPosition.x + (x - GeneratedAreaSize / 2), aroundPosition.y + (y - GeneratedAreaSize / 2));
                    if (_chunkGrid.ChunkMap.ContainsKey(pos))
                        continue;

                    var chunk = new Chunk(pos);
                    _chunkGrid.ChunkMap.Add(pos, chunk);

                    var generateJob = new GenerationTask
                    {
                        ChunkPos = pos,
                        BlockColors = chunk.BlockColors,
                        BlockTypes = chunk.BlockTypes
                    };

                    var handle = Task.Run(() => generateJob.Execute());
                    generateJobs.Add(generateJob);
                    generateTasks.Add(handle);
                    chunk.GameObject = Instantiate(ChunkPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    chunk.GameObject.transform.parent = ParentChunkObject.transform;
                    chunk.GameObject.SetActive(true);
                }
            }

            for (var i = 0; i < generateJobs.Count; ++i)
            {
                var handle = generateTasks[i];
                var chunk = _chunkGrid.ChunkMap[generateJobs[i].ChunkPos];
                handle.Wait();
                var texture = new Texture2D(Chunk.Size, Chunk.Size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;
                texture.LoadRawTextureData(chunk.BlockColors);
                texture.Apply();
                chunk.GameObject.GetComponent<SpriteRenderer>().sprite = Sprite.Create(
                    texture,
                    new Rect(new Vector2(0, 0), new Vector2(Chunk.Size, Chunk.Size)), new Vector2(0.5f, 0.5f),
                    Chunk.Size, 
                    0,
                    SpriteMeshType.FullRect);
                chunk.Texture = texture;
            }
        }

        private void Clean(Vector2 aroundPosition)
        {
            var px = aroundPosition.x - GeneratedAreaSize / 2;
            var py = aroundPosition.y - GeneratedAreaSize / 2;

            var chunksToRemove = new List<Vector2>();
            foreach (var chunk in _chunkGrid.ChunkMap.Values)
            {
                if (chunk.Position.x < px - CleanAreaSizeOffset || chunk.Position.x > px + GeneratedAreaSize + CleanAreaSizeOffset
                || chunk.Position.y < py - CleanAreaSizeOffset || chunk.Position.y > py + GeneratedAreaSize + CleanAreaSizeOffset)
                {
                    chunksToRemove.Add(chunk.Position);
                    chunk.Dispose();
                }
            }

            foreach (var chunkPosition in chunksToRemove)
            {
                _chunkGrid.ChunkMap.Remove(chunkPosition);
            }
        }

        private void Simulate()
        {
            var batchPool = new List<List<Task>>(BatchNumber);
            for (var i = 0; i < BatchNumber; ++i)
            {
                batchPool.Add(new List<Task>());
            }

            foreach (var chunk in _chunkGrid.ChunkMap.Values)
            {
                var chunkPos = chunk.Position;
                if (chunkPos.x % 2 == 0 && chunkPos.y % 2 == 0)
                    batchPool[0].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
                else if(chunkPos.x % 2 == 0 && chunkPos.y % 2 == 1)
                    batchPool[1].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
                else if(chunkPos.x % 2 == 1 && chunkPos.y % 2 == 0)
                    batchPool[2].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
                else
                    batchPool[3].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
            }
            
            foreach (var batch in batchPool)
            {
                foreach (var task in batch)
                {
                    if (GlobalConfig.StaticGlobalConfig.MonothreadSimulate)
                    {
                        task.RunSynchronously();
                    }
                    else
                    {
                        task.Start();
                    }
                }

                if (!GlobalConfig.StaticGlobalConfig.MonothreadSimulate)
                {
                    foreach (var task in batch)
                    {
                        task.Wait();
                    }
                }
            }

            foreach (var chunk in _chunkGrid.ChunkMap.Values)
            {
                chunk.Texture.LoadRawTextureData(chunk.BlockColors);
                chunk.Texture.Apply();
            }
        }

        private Task CreateSimulateTaskForChunkNeighborhood(Chunk chunk)
        {
            var chunks = new[]
            {
                chunk,
                GetNeighborChunkBlocksColors(chunk, -1, -1),
                GetNeighborChunkBlocksColors(chunk, 0, -1),
                GetNeighborChunkBlocksColors(chunk, 1, -1),
                GetNeighborChunkBlocksColors(chunk, -1, 0),
                GetNeighborChunkBlocksColors(chunk, 1, 0),
                GetNeighborChunkBlocksColors(chunk, -1, 1),
                GetNeighborChunkBlocksColors(chunk, 0, 1),
                GetNeighborChunkBlocksColors(chunk, 1, 1)
            };
            return new Task(() => new SimulationTask(chunks, RandomArray).Execute());
        }

        private Chunk GetNeighborChunkBlocksColors(Chunk origin, int xOffset, int yOffset)
        {
            var neighborPosition = new Vector2(origin.Position.x + xOffset, origin.Position.y + yOffset);
            if (_chunkGrid.ChunkMap.ContainsKey(neighborPosition))
            {
                return _chunkGrid.ChunkMap[neighborPosition];
            }
            return null;
        }

        private void OnDestroy()
        {
            _chunkGrid.Dispose();
        }
    }
}
