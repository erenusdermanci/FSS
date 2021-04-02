using System.IO;
using DebugTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using static Chunks.ChunkLayer;

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

        public static string GetChunksSavePath(ChunkLayerType layerType)
        {
            return $"{GetSavePath()}\\{SceneManager.GetActiveScene().name}\\{layerType.ToString()}";
        }

        public static string GetChunksSaveFullPath(ChunkLayerType layerType, Vector2i position)
        {
            return $"{GetChunksSavePath(layerType)}\\{GetChunkSaveName(position)}";
        }

        public static bool IsChunkPersisted(ChunkLayerType chunkLayerType, Vector2i position)
        {
            return File.Exists(GetChunksSaveFullPath(chunkLayerType, position));
        }

        public static T GetNeighborChunk<T>(ChunkMap<T> chunkMap, T origin, int xOffset, int yOffset) where T : Chunk
        {
            var neighborPosition = new Vector2i(origin.Position.x + xOffset, origin.Position.y + yOffset);
            return chunkMap.Contains(neighborPosition) ? chunkMap[neighborPosition] : null;
        }
    }
}
