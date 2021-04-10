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
    public class TileSaveTask : TileTask
    {
        public TileSaveTask(Tile tile, ChunkLayer[] chunkLayers) : base(tile, chunkLayers)
        {

        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            var tileData = new TileData
            {
                chunkLayers = new BlockData[ChunkLayer.TotalChunkLayers][]
            };

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                ChunksForMainThread[i] = new List<ChunkServer>();
                var idx = 0;
                tileData.chunkLayers[i] = new BlockData[Tile.TotalSize];
                foreach (var position in Tile.GetChunkPositions())
                {
                    if (ChunkLayers[i].ServerChunkMap.Contains(position))
                    {
                        var chunkToSave = ChunkLayers[i].ServerChunkMap[position];
                        // ReSharper disable once PossibleNullReferenceException
                        tileData.chunkLayers[i][idx] = chunkToSave.Data;
                        ChunksForMainThread[i].Add(chunkToSave);
                    }
                    idx++;
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
