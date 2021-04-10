using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks;
using Chunks.Server;
using Serialized;
using Utils;

namespace Tiles.Tasks
{
    public class TileLoadTask : TileTask
    {
        public TileLoadTask(Tile tile, ChunkLayer[] chunkLayers) : base(tile, chunkLayers)
        {

        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            if (File.Exists(TileFileName))
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
            using (var file = File.Open(TileFileName, FileMode.Open))
            using (var compressionStream = new GZipStream(file, CompressionMode.Decompress))
            {
                var loadedData = new BinaryFormatter().Deserialize(compressionStream);
                compressionStream.Flush();
                tileData = (TileData) loadedData;
            }

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                ChunksForMainThread[i] = new List<ChunkServer>();
                var idx = 0;
                foreach (var position in Tile.GetChunkPositions())
                {
                    var chunk = new ChunkServer
                    {
                        Position = position, Data = tileData.chunkLayers[i][idx]
                    };
                    ChunkLayers[i].ServerChunkMap.Add(chunk);
                    ChunksForMainThread[i].Add(chunk);
                    idx++;
                }
            }
        }

        private void GenerateEmpty()
        {
            // tile does not exist, generate empty chunks instead
            // temporary measure until we have a pre-compiled world file
            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                ChunksForMainThread[i] = new List<ChunkServer>();

                foreach (var position in Tile.GetChunkPositions())
                {
                    var emptyChunk = new ChunkServer
                    {
                        Position = position
                    };
                    emptyChunk.Initialize();
                    emptyChunk.GenerateEmpty();

                    if (ChunkLayers[i].ServerChunkMap.Contains(position))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        ChunkLayers[i].ServerChunkMap[position].Data = emptyChunk.Data;
                    }
                    else
                    {
                        ChunkLayers[i].ServerChunkMap.Add(emptyChunk);
                    }

                    ChunksForMainThread[i].Add(emptyChunk);
                }
            }
        }
    }
}
