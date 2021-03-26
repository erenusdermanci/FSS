using System.Collections.Generic;
using Chunks.Tasks;
using DebugTools;
using Utils;

namespace Chunks.Server
{
    public sealed class ChunkServerTaskScheduler
    {
        private readonly Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>> _chunkTaskManagers;

        public ChunkServerTaskScheduler()
        {
            _chunkTaskManagers = new Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>>
            {
                {
                    ChunkTaskTypes.Save,
                    new ChunkTaskManager<ChunkServer>(16, chunk => new SaveTask(chunk))
                },
                {
                    ChunkTaskTypes.Load,
                    new ChunkTaskManager<ChunkServer>(16, chunk => new LoadTask(chunk))
                },
                {
                    ChunkTaskTypes.Generate,
                    new ChunkTaskManager<ChunkServer>(16, chunk => new GenerationTask(chunk))
                }
            };
        }

        public ChunkTaskManager<ChunkServer> GetTaskManager(ChunkTaskTypes chunkTaskType)
        {
            return _chunkTaskManagers[chunkTaskType];
        }

        public void Update()
        {
            foreach (var chunkTaskManager in _chunkTaskManagers.Values)
            {
                chunkTaskManager.Update();
            }
        }

        public void QueueForGeneration(Vector2i pos, bool loadFromDisk)
        {
            if (_chunkTaskManagers[ChunkTaskTypes.Generate].Pending(pos))
                return;
            if (_chunkTaskManagers[ChunkTaskTypes.Load].Pending(pos)) // chunk is already being loaded or queued for loading
                return;
            if (_chunkTaskManagers[ChunkTaskTypes.Save].Pending(pos)) // chunk is being saved or queued for saving
            {
                // if it was queued we have a chance to remove it so that we don't take the time to save before loading
                _chunkTaskManagers[ChunkTaskTypes.Save].Cancel(pos);
            }

            if (!GlobalDebugConfig.StaticGlobalConfig.disableLoad
                && loadFromDisk
                && ChunkHelpers.IsChunkPersisted(pos))
            {
                _chunkTaskManagers[ChunkTaskTypes.Load].Enqueue(pos);
            }
            else
            {
                _chunkTaskManagers[ChunkTaskTypes.Generate].Enqueue(pos);
            }
        }

        public void QueueForSaving(ChunkServer chunk)
        {
            _chunkTaskManagers[ChunkTaskTypes.Save].Enqueue(chunk);
        }

        public void CancelGeneration()
        {
            _chunkTaskManagers[ChunkTaskTypes.Generate].CancelAll();
        }

        public void CancelLoading()
        {
            _chunkTaskManagers[ChunkTaskTypes.Load].CancelAll();
        }

        public void ForceSaving()
        {
            _chunkTaskManagers[ChunkTaskTypes.Save].CompleteAll();
        }
    }
}
