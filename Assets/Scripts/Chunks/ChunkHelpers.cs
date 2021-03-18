using System.IO;
using DebugTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Chunks
{
    public static class ChunkHelpers
    {
        public const int FileSaveBufferSize = Chunk.Size * Chunk.Size
                                                          * (sizeof(byte) * 4 // block colors
                                                             + sizeof(int)); // block types

        public const string TestSceneFolder = "TestScenes";

        public static string GetChunkSaveName(Vector2i position)
        {
            return $"{position.x:x8}{position.y:x8}";
        }

        public static string GetSavePath()
        {
            return GlobalDebugConfig.StaticGlobalConfig.saveAsTestScene
                ? $"{Application.dataPath}\\..\\{TestSceneFolder}"
                : $"{Application.persistentDataPath}";
        }

        public static string GetChunksSavePath()
        {
            return $"{GetSavePath()}\\{SceneManager.GetActiveScene().name}";
        }

        public static string GetChunksSaveFullPath(Vector2i position)
        {
            return $"{GetChunksSavePath()}\\{GetChunkSaveName(position)}";
        }

        public static bool IsChunkPersisted(Vector2i position)
        {
            return File.Exists(GetChunksSaveFullPath(position));
        }

        public static Chunk GetNeighborChunk(ChunkMap chunkMap, Chunk origin, int xOffset, int yOffset)
        {
            var neighborPosition = new Vector2i(origin.Position.x + xOffset, origin.Position.y + yOffset);
            return chunkMap.Contains(neighborPosition) ? chunkMap[neighborPosition] : null;
        }
    }
}