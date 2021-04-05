﻿using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks;
using Chunks.Server;
using Serialized;
using Utils;

namespace Tiles.Tasks
{
    public class LoadTask : TileTask
    {
        public LoadTask(Tile tile, ChunkLayer[] chunkLayers) : base(tile, chunkLayers)
        {

        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            if (File.Exists(tileFileName))
            {
                LoadExisting();
            }
            else
            {
                GenerateEmpty();
            }
        }

        private void LoadExisting()
        {
            TileData tileData;
            using (var file = File.Open(tileFileName, FileMode.Open))
            using (var compressionStream = new GZipStream(file, CompressionMode.Decompress))
            {
                var loadedData = new BinaryFormatter().Deserialize(compressionStream);
                compressionStream.Flush();
                tileData = (TileData) loadedData;
            }

            // Set all the contained chunks
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunksForMainThread[i] = new List<ChunkServer>();
                var idx = 0;
                for (var y = Tile.TilePosition.y * Tile.VerticalSize; y < Tile.TilePosition.y * Tile.VerticalSize + Tile.VerticalSize; ++y)
                {
                    for (var x = Tile.TilePosition.x * Tile.HorizontalSize; x < Tile.TilePosition.x * Tile.HorizontalSize + Tile.HorizontalSize; ++x)
                    {
                        var posVec = new Vector2i(x, y);
                        var chunk = new ChunkServer
                        {
                            Position = posVec, Data = tileData.chunkLayers[i][idx]
                        };
                        chunkLayers[i].ServerChunkMap.Add(chunk);

                        chunksForMainThread[i].Add(chunk);

                        idx++;
                    }
                }
            }
        }

        private void GenerateEmpty()
        {
            // tile does not exist, generate empty chunks instead
            // temporary measure until we have a pre-compiled world file
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunksForMainThread[i] = new List<ChunkServer>();
                for (var y = Tile.TilePosition.y * Tile.VerticalSize; y < Tile.TilePosition.y * Tile.VerticalSize + Tile.VerticalSize; ++y)
                {
                    for (var x = Tile.TilePosition.x * Tile.HorizontalSize; x < Tile.TilePosition.x * Tile.HorizontalSize + Tile.HorizontalSize; ++x)
                    {
                        var posVec = new Vector2i(x, y);
                        var emptyChunk = new ChunkServer {Position = posVec};
                        emptyChunk.Initialize();
                        emptyChunk.GenerateEmpty();

                        if (chunkLayers[i].ServerChunkMap.Contains(posVec))
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            chunkLayers[i].ServerChunkMap[posVec].Data = emptyChunk.Data;
                        }
                        else
                        {
                            chunkLayers[i].ServerChunkMap.Add(emptyChunk);
                        }

                        chunksForMainThread[i].Add(emptyChunk);
                    }
                }
            }
        }
    }
}
