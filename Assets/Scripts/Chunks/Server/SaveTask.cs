using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks.Tasks;
using UnityEngine;
using static Chunks.ChunkLayer;

namespace Chunks.Server
{
    public class SaveTask : ChunkTask<ChunkServer>
    {
        private readonly string _chunkSavePath;
        private readonly string _chunkSaveFullPath;

        public SaveTask(ChunkServer chunk, ChunkLayerType layerType) : base(chunk, layerType)
        {
            _chunkSavePath = ChunkHelpers.GetChunksSavePath();
            _chunkSaveFullPath = ChunkHelpers.GetChunksSaveFullPath(LayerType, Chunk.Position);
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;
            try
            {
                if (!Directory.Exists(_chunkSavePath))
                {
                    Directory.CreateDirectory(_chunkSavePath);
                }

                using (var file = File.Create(_chunkSaveFullPath, ChunkHelpers.FileSaveBufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous))
                {
                    new BinaryFormatter().Serialize(file, Chunk.Data);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }
    }
}
