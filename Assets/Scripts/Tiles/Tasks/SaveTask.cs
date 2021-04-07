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
    public class SaveTask : TileTask
    {
        public SaveTask(Tile tile, ChunkLayer[] chunkLayers) : base(tile, chunkLayers)
        {

        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            var tileData = new TileData {chunkLayers = new BlockData[Tile.LayerCount][]};

            // Serialize all the contained chunks
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                ChunksForMainThread[i] = new List<ChunkServer>();
                var idx = 0;
                tileData.chunkLayers[i] = new BlockData[Tile.VerticalSize * Tile.HorizontalSize];
                for (var y = Tile.TilePosition.y * Tile.VerticalSize; y < Tile.TilePosition.y * Tile.VerticalSize + Tile.VerticalSize; ++y)
                {
                    for (var x = Tile.TilePosition.x * Tile.HorizontalSize; x < Tile.TilePosition.x * Tile.HorizontalSize + Tile.HorizontalSize; ++x)
                    {
                        var posVec = new Vector2i(x, y);
                        if (ChunkLayers[i].ServerChunkMap.Contains(posVec))
                        {
                            var chunkToSave = ChunkLayers[i].ServerChunkMap[posVec];
                            // ReSharper disable once PossibleNullReferenceException
                            tileData.chunkLayers[i][idx] = chunkToSave.Data;

                            ChunksForMainThread[i].Add(chunkToSave);
                        }

                        idx++;
                    }
                }
            }

            using (var file = File.Open(TileFileName, FileMode.Create))
            using (var compressionStream = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressionStream, tileData);
                compressionStream.Flush();
            }
        }
    }
}
