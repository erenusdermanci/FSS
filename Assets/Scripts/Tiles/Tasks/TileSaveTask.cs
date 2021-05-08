using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Serialized;

namespace Tiles.Tasks
{
    public class TileSaveTask : TileTask
    {
        public TileSaveTask(Tile tile) : base(tile)
        {
            TileData = new TileData
            {
                // chunks = new BlockData[Tile.ChunkAmount],
            };
        }

        protected override void Execute()
        {
            using (var file = File.Open(TileFullFileName, FileMode.Create))
            using (var compressionStream = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressionStream, TileData);
                compressionStream.Flush();
            }
        }
    }
}
