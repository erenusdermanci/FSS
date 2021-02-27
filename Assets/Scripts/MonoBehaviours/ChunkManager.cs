using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataComponents;
using Jobs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoBehaviours
{
    public class ChunkManager : MonoBehaviour
    {
        public int GeneratedAreaSize = 10;
        public int CleanAreaSizeOffset = 2;
        public GameObject ChunkPrefab;
        public Transform PlayerTransform;

        private ChunkGrid _chunkGrid;
        private const int BatchNumber = 4;

        private void Awake()
        {
            _chunkGrid = new ChunkGrid();
            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
        }

        private void ProceduralGeneratorUpdate(object sender, EventArgs e)
        {
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            _chunkGrid.Dispose();
            _chunkGrid = new ChunkGrid();
            Generate(flooredAroundPosition);
        }

        private void FixedUpdate()
        {
            var flooredAroundPosition = new Vector2(Mathf.Floor(PlayerTransform.position.x), Mathf.Floor(PlayerTransform.position.y));
            Generate(flooredAroundPosition);
            Clean(flooredAroundPosition);
            Simulate();
        }

        private void Generate(Vector2 aroundPosition)
        {
            var generateJobs = new List<GenerateJob>();
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

                    var generateJob = new GenerateJob
                    {
                        ChunkPos = pos,
                        BlockColors = chunk.BlockColors,
                        BlockTypes = chunk.BlockTypes
                    };

                    var handle = Task.Run(() => generateJob.Execute());
                    generateJobs.Add(generateJob);
                    generateTasks.Add(handle);
                    chunk.GameObject = Instantiate(ChunkPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    chunk.GameObject.SetActive(true);
                }
            }

            for (var i = 0; i < generateJobs.Count; ++i)
            {
                var handle = generateTasks[i];
                var chunk = _chunkGrid.ChunkMap[generateJobs[i].ChunkPos];
                handle.Wait();
                var texture = new Texture2D(Chunk.Size, Chunk.Size, TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(chunk.BlockColors);
                texture.Apply();
                chunk.GameObject.GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(new Vector2(0, 0), new Vector2(Chunk.Size, Chunk.Size)), new Vector2(0.5f, 0.5f), Chunk.Size, 0, SpriteMeshType.FullRect);
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
                    batchPool[0].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
                else if(chunkPos.x % 2 == 1 && chunkPos.y % 2 == 0)
                    batchPool[0].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
                else
                    batchPool[0].Add(CreateSimulateTaskForChunkNeighborhood(chunk));
            }
            
            foreach (var batch in batchPool)
            {
                foreach (var task in batch)
                {
                    task.Start();
                }
            
                foreach (var task in batch)
                {
                    task.Wait();
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
            return new Task(() => new ChunkNeighborhood(chunks).Simulate());
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
