using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace DataComponents
{
    public static class ChunkHelpers
    {
        public static string GetChunkSaveName(Vector2 position)
        {
            return $"{(int)position.x:x8}{(int)position.y:x8}";
        }

        public static string GetChunksSaveFullPath(Vector2 position)
        {
            return Application.persistentDataPath + "\\" + GetChunkSaveName(position);
        }

        public static void Save(this Chunk chunk)
        {
            var fullPath = GetChunksSaveFullPath(chunk.Position);

            using (var file = File.Create(fullPath))
            {
                new BinaryFormatter().Serialize(file, chunk.blockData);
            }
        }

        public static Chunk Load(Vector2 position)
        {
            var fullPath = GetChunksSaveFullPath(position);
            if (!File.Exists(fullPath))
            {
                return null;
            }
            using (var file = File.Open(fullPath, FileMode.Open))
            {
                var loadedData = new BinaryFormatter().Deserialize(file);
                var data = (Chunk.BlockData)loadedData;
                var chunk = new Chunk(data)
                {
                    Position = new Vector2(position.x, position.y)
                };
                return chunk;
            }
        }
    }
}