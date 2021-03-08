using System.Collections.Concurrent;
using System.IO;
using MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DataComponents
{
    public static class ChunkHelpers
    {
        public const int FileSaveBufferSize = Chunk.Size * Chunk.Size
                                                          * (sizeof(byte) * 4 // block colors
                                                             + sizeof(int)); // block types

        public const string TestSceneFolder = "TestScenes";

        public static string GetChunkSaveName(Vector2 position)
        {
            return $"{(int)position.x:x8}{(int)position.y:x8}";
        }

        public static string GetSavePath()
        {
            return GlobalDebugConfig.StaticGlobalConfig.SaveAsTestScene
                ? $"{Application.dataPath}\\..\\{TestSceneFolder}"
                : $"{Application.persistentDataPath}";
        }

        public static string GetChunksSavePath(Vector2 position)
        {
            return $"{GetSavePath()}\\{SceneManager.GetActiveScene().name}";
        }

        public static string GetChunksSaveFullPath(Vector2 position)
        {
            return $"{GetChunksSavePath(position)}\\{GetChunkSaveName(position)}";
        }

        public static bool IsChunkPersisted(Vector2 position)
        {
            return File.Exists(GetChunksSaveFullPath(position));
        }

        public static Chunk GetNeighborChunk(ConcurrentDictionary<Vector2, Chunk> chunkMap, Chunk origin, int xOffset, int yOffset)
        {
            var neighborPosition = new Vector2(origin.Position.x + xOffset, origin.Position.y + yOffset);
            return chunkMap.ContainsKey(neighborPosition) ? chunkMap[neighborPosition] : null;
        }
    }
}