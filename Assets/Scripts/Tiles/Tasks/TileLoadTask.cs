using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Serialized;

namespace Tiles.Tasks
{
    public class TileLoadTask : TileTask
    {
        public TileLoadTask(Tile tile) : base(tile)
        {

        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            if (File.Exists(TileFullFileName))
            {
                LoadExisting();
            }
            else
            {
                if (TileHelpers.TilesInitialLoadPath == TileHelpers.TilesSavePath)
                    return;
                TileFullFileName = $"{TileHelpers.TilesInitialLoadPath}\\{TileFileName}";
                if (File.Exists(TileFullFileName))
                {
                    LoadExisting();
                }
            }
        }

        private void LoadExisting()
        {
            using (var file = File.Open(TileFullFileName, FileMode.Open))
            using (var compressionStream = new GZipStream(file, CompressionMode.Decompress))
            {
                var loadedData = new BinaryFormatter().Deserialize(compressionStream);
                compressionStream.Flush();
                TileData = (TileData) loadedData;
            }
        }
    }
}
