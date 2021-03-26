using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Chunks.Tasks;
using Serialized;
using UnityEngine;

namespace Chunks.Server
{
    public class LoadTask : ChunkTask<ChunkServer>
    {
        private readonly string _chunkSaveFullPath;

        public LoadTask(ChunkServer chunk) : base(chunk)
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
                    Chunk.Data = (BlockData)loadedData;
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
