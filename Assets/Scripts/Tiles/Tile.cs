using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using Serialized;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Utils.UnityHelpers;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int HorizontalSize = 4;
        public const int VerticalSize = 3;
        public const int LayerCount = 2;

        private readonly string _tileDir = $"{Application.persistentDataPath}\\{SceneManager.GetActiveScene().name}\\Tiles";
        private readonly string _tileFileName;

        public Vector2i TilePosition;

        public Tile(Vector2i position)
        {
            TilePosition = position;
            _tileFileName = $"{_tileDir}\\{TilePosition.x}_{TilePosition.y}";
            if (!Directory.Exists(_tileDir))
                Directory.CreateDirectory(_tileDir);
        }

        public void Dispose()
        {

        }

        public void Load(IReadOnlyList<ChunkMap<ChunkServer>> chunkServerMaps,
            IReadOnlyList<ChunkMap<ChunkClient>> chunkClientMaps,
            ChunkLayerSimulator[] simulators,
            GameObjectPool[] chunkPools)
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
                    for (var y = TilePosition.y * VerticalSize; y < TilePosition.y * VerticalSize + VerticalSize; ++y)
                    {
                        for (var x = TilePosition.x * HorizontalSize; x < TilePosition.x * HorizontalSize + HorizontalSize; ++x)
                        {
                            var posVec = new Vector2i(x, y);
                            if (chunkServerMaps[i].Contains(posVec)) // should not happen
                            {
                                Debug.Log("weird situation");
                                // ReSharper disable once PossibleNullReferenceException
                                chunkServerMaps[i][posVec].Data = tileData.chunkLayers[i][idx];
                                chunkServerMaps[i][posVec].Dirty = true;
                            }
                            else
                            {
                                var chunk = new ChunkServer
                                {
                                    Position = posVec, Dirty = true, Data = tileData.chunkLayers[i][idx]
                                };
                                chunkServerMaps[i].Add(chunk);
                                if (simulators[i] != null)
                                    simulators[i].UpdateSimulationPool(chunk, true);
                                chunkClientMaps[i].Add(CreateClientChunk(chunkPools[i], chunk));
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
                    for (var y = TilePosition.y * VerticalSize; y < TilePosition.y * VerticalSize + VerticalSize; ++y)
                    {
                        for (var x = TilePosition.x * HorizontalSize; x < TilePosition.x * HorizontalSize + HorizontalSize; ++x)
                        {
                            var posVec = new Vector2i(x, y);
                            var emptyChunk = new ChunkServer {Position = posVec};
                            emptyChunk.Initialize();
                            emptyChunk.GenerateEmpty();

                            if (chunkServerMaps[i].Contains(posVec))
                            {
                                // ReSharper disable once PossibleNullReferenceException
                                chunkServerMaps[i][posVec].Data = emptyChunk.Data;
                            }
                            else
                            {
                                chunkServerMaps[i].Add(emptyChunk);
                                if (simulators[i] != null)
                                    simulators[i].UpdateSimulationPool(emptyChunk, true);
                                chunkClientMaps[i].Add(CreateClientChunk(chunkPools[i], emptyChunk));
                            }
                        }
                    }
                }
            }

        }

        public void Save(IReadOnlyList<ChunkMap<ChunkServer>> chunkServerMaps,
            IReadOnlyList<ChunkMap<ChunkClient>> chunkClientMaps)
        {
            var tileData = new TileData {chunkLayers = new BlockData[LayerCount][]};

            // Serialize all the contained chunks
            for (var i = 0; i < LayerCount; ++i)
            {
                var idx = 0;
                tileData.chunkLayers[i] = new BlockData[VerticalSize * HorizontalSize];
                for (var y = TilePosition.y * VerticalSize; y < TilePosition.y * VerticalSize + VerticalSize; ++y)
                {
                    for (var x = TilePosition.x * HorizontalSize; x < TilePosition.x * HorizontalSize + HorizontalSize; ++x)
                    {
                        var posVec = new Vector2i(x, y);
                        if (chunkServerMaps[i].Contains(posVec))
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            tileData.chunkLayers[i][idx] = chunkServerMaps[i][posVec].Data;

                            // remove the chunks from the chunkmap
                            chunkServerMaps[i][posVec].Dispose();
                            chunkServerMaps[i].Remove(posVec);

                            chunkClientMaps[i][posVec].Dispose();
                            chunkClientMaps[i].Remove(posVec);
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

        private ChunkClient CreateClientChunk(GameObjectPool chunkPool, ChunkServer chunkServer)
        {
            var chunkGameObject = chunkPool.GetObject();
            chunkGameObject.transform.position = new Vector3(chunkServer.Position.x, chunkServer.Position.y, 0);
            var clientChunk = new ChunkClient
            {
                Position = chunkServer.Position,
                Colors = chunkServer.Data.colors, // pointer on ChunkServer colors,
                Types = chunkServer.Data.types, // pointer on ChunkServer types,
                GameObject = chunkGameObject,
                Collider = chunkGameObject.GetComponent<PolygonCollider2D>(),
                Texture = chunkGameObject.GetComponent<SpriteRenderer>().sprite.texture
            };
            chunkGameObject.SetActive(true);
            clientChunk.UpdateTexture();

            return clientChunk;
        }
    }
}
