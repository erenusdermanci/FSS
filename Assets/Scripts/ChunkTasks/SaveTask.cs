using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using DataComponents;
using UnityEngine;

namespace ChunkTasks
{
    public class SaveTask : ChunkTask
    {
        private readonly string _chunkSavePath;
        private readonly string _chunkSaveFullPath;
        
        public SaveTask(Chunk chunk) : base(chunk)
        {
            _chunkSavePath = ChunkHelpers.GetChunksSavePath();
            _chunkSaveFullPath = ChunkHelpers.GetChunksSaveFullPath(Chunk.Position);
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
                    new BinaryFormatter().Serialize(file, Chunk.blockData);
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