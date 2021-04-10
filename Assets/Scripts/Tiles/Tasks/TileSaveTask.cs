using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks;
using Serialized;

namespace Tiles.Tasks
{
    public class TileSaveTask : TileTask
    {
        public TileSaveTask(Tile tile) : base(tile)
        {
            TileData = new TileData
            {
                chunkLayers = new BlockData[ChunkLayer.TotalChunkLayers][]
            };

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                TileData.Value.chunkLayers[i] = new BlockData[Tile.TotalSize];
            }
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            using (var file = File.Open(TileFullFileName, FileMode.Create))
            using (var compressionStream = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressionStream, TileData.Value);
                compressionStream.Flush();
            }
        }
    }
}
