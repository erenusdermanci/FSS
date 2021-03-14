﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Chunks.Tasks
{
    public class LoadTask : ChunkTask
    {
        private readonly string _chunkSaveFullPath;

        public LoadTask(Chunk chunk) : base(chunk)
        {
            _chunkSaveFullPath = ChunkHelpers.GetChunksSaveFullPath(Chunk.Position);
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;
            try
            {
                using (var file = File.Open(_chunkSaveFullPath, FileMode.Open))
                {
                    var loadedData = new BinaryFormatter().Deserialize(file);
                    Chunk.Data = (Chunk.BlockData)loadedData;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }

            Chunk.Dirty = true;
        }
    }
}