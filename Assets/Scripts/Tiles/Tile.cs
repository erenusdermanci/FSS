using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks;
using Chunks.Server;
using Serialized;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int Size = 4;
        public const int LayerCount = 2;

        public List<Vector2i> ChunkPositions;

        private readonly string _tileDir = $"{Application.persistentDataPath}\\{SceneManager.GetActiveScene().name}\\Tiles";
        private readonly string _tileFileName;

        public Vector2i TilePosition;
        public ChunkLayer.ChunkLayerType LayerType;

        public Tile(Vector2i position)
        {
            TilePosition = position;
            _tileFileName = $"{_tileDir}\\{TilePosition.x}_{TilePosition.y}";
            ChunkPositions = EnumerateChunkPositions().ToList();
            if (!Directory.Exists(_tileDir))
                Directory.CreateDirectory(_tileDir);
        }

        public Tile(Vector2i position, ChunkLayer.ChunkLayerType layerType)
        {
            TilePosition = position;
            LayerType = layerType;
            _tileFileName = $"{_tileDir}\\{LayerType.ToString()}\\{TilePosition.x}_{TilePosition.y}";
        }

        public void Dispose()
        {

        }

        public void Load(IReadOnlyList<ChunkMap<ChunkServer>> chunkMaps, ChunkLayerSimulator[] simulators)
        {
            if (File.Exists(_tileFileName))
            {
                TileData tileData;
                using (var file = File.Open(_tileFileName, FileMode.Open))
                using (var compressionStream = new GZipStream(file, CompressionMode.Decompress))
                {
                    var loadedData = new BinaryFormatter().Deserialize(compressionStream);
                    compressionStream.Flush();
                    tileData = (TileData) loadedData;
                }

                // Set all the contained chunks
                for (var i = 0; i < LayerCount; ++i)
                {
                    var idx = 0;
                    for (var y = TilePosition.y * Size; y < TilePosition.y * Size + Size; ++y)
                    {
                        for (var x = TilePosition.x * Size; x < TilePosition.x * Size + Size; ++x)
                        {
                            var posVec = new Vector2i(x, y);
                            if (chunkMaps[i].Contains(posVec))
                            {
                                // ReSharper disable once PossibleNullReferenceException
                                chunkMaps[i][posVec].Data = tileData.chunkLayers[i][idx];
                                chunkMaps[i][posVec].Dirty = true;
                            }
                            else
                            {
                                var chunk = new ChunkServer
                                {
                                    Position = posVec, Dirty = true, Data = tileData.chunkLayers[i][idx]
                                };
                                chunkMaps[i].Add(chunk);
                                if (simulators[i] != null)
                                    simulators[i].UpdateSimulationPool(chunk, true);
                            }

                            idx++;
                        }
                    }
                }
            }
            else
            {
                // tile does not exist, generate empty chunks instead

                for (var i = 0; i < LayerCount; ++i)
                {
                    for (var y = TilePosition.y * Size; y < TilePosition.y * Size + Size; ++y)
                    {
                        for (var x = TilePosition.x * Size; x < TilePosition.x * Size + Size; ++x)
                        {
                            var posVec = new Vector2i(x, y);
                            var emptyChunk = new ChunkServer {Position = posVec};
                            emptyChunk.Initialize();
                            emptyChunk.GenerateEmpty();

                            if (chunkMaps[i].Contains(posVec))
                            {
                                // ReSharper disable once PossibleNullReferenceException
                                chunkMaps[i][posVec].Data = emptyChunk.Data;
                            }
                            else
                            {
                                chunkMaps[i].Add(emptyChunk);
                                if (simulators[i] != null)
                                    simulators[i].UpdateSimulationPool(emptyChunk, true);
                            }
                        }
                    }
                }
            }

        }

        public void Save(IReadOnlyList<ChunkMap<ChunkServer>> chunkMaps)
        {
            var tileData = new TileData {chunkLayers = new BlockData[LayerCount][]};


            // Serialize all the contained chunks
            for (var i = 0; i < LayerCount; ++i)
            {
                var idx = 0;
                tileData.chunkLayers[i] = new BlockData[Size * Size];
                for (var y = TilePosition.y * Size; y < TilePosition.y * Size + Size; ++y)
                {
                    for (var x = TilePosition.x * Size; x < TilePosition.x * Size + Size; ++x)
                    {
                        var posVec = new Vector2i(x, y);
                        if (chunkMaps[i].Contains(posVec))
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            tileData.chunkLayers[i][idx] = chunkMaps[i][posVec].Data;
                        }

                        idx++;
                    }
                }
            }

            using (var file = File.Open(_tileFileName, FileMode.Create))
            using (var compressionStream = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressionStream, tileData);
                compressionStream.Flush();
            }
        }

        public IEnumerable<Vector2i> EnumerateChunkPositions()
        {
            var positions = new Vector2i[Size * Size];
            var idx = 0;

            for (var y = TilePosition.y * Size; y < TilePosition.y * Size + Size; ++y)
            {
                for (var x = TilePosition.x * Size; x < TilePosition.x * Size + Size; ++x)
                {
                    positions[idx++] = new Vector2i(x, y);
                }
            }

            return positions;
        }
    }
}
