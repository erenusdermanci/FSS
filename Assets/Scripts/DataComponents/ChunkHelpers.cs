using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataComponents
{
    public static class ChunkHelpers
    {
        private const int FileSaveBufferSize = Chunk.Size * Chunk.Size
                                                          * (sizeof(byte) * 4 // block colors
                                                             + sizeof(int)); // block types

        private const string TestSceneFolder = "TestScenes";

        private static string GetChunkSaveName(Vector2 position)
        {
            return $"{(int)position.x:x8}{(int)position.y:x8}";
        }

        private static string GetSavePath()
        {
            return GlobalDebugConfig.StaticGlobalConfig.SaveAsTestScene
                ? $"{Application.dataPath}\\..\\{TestSceneFolder}"
                : $"{Application.persistentDataPath}";
        }

        private static string GetChunksSavePath(Vector2 position)
        {
            return $"{GetSavePath()}\\{SceneManager.GetActiveScene().name}";
        }

        private static string GetChunksSaveFullPath(Vector2 position)
        {
            return $"{GetChunksSavePath(position)}\\{GetChunkSaveName(position)}";
        }

        public static void Save(this Chunk chunk)
        {
            var savePath = GetChunksSavePath(chunk.Position);

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            using (var file = File.Create(GetChunksSaveFullPath(chunk.Position), FileSaveBufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous))
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