using System;
using System.Collections.Generic;
using System.Threading;
using DebugTools;
using UnityEngine;
using Random = System.Random;

namespace Chunks.Tasks
{
    public sealed class ChunkTaskScheduler
    {
        private readonly Dictionary<ChunkTaskManager.Types, ChunkTaskManager> _chunkTaskManagers;

        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() =>
            new Random(new Random((int) DateTimeOffset.Now.ToUnixTimeMilliseconds()).Next()));

        public ChunkTaskScheduler()
        {
            _chunkTaskManagers = new Dictionary<ChunkTaskManager.Types, ChunkTaskManager>
            {
                {
                    ChunkTaskManager.Types.Save,
                    new ChunkTaskManager(16, chunk => new SaveTask(chunk))
                },
                {
                    ChunkTaskManager.Types.Load,
                    new ChunkTaskManager(16, chunk => new LoadTask(chunk))
                },
                {
                    ChunkTaskManager.Types.Generate,
                    new ChunkTaskManager(16, chunk => new GenerationTask(chunk)
                    {
                        Rng = _random
                    })
                }
            };
        }

        public ChunkTaskManager GetTaskManager(ChunkTaskManager.Types type)
        {
            return _chunkTaskManagers[type];
        }

        public void Update()
        {
            foreach (var chunkTaskManager in _chunkTaskManagers.Values)
            {
                chunkTaskManager.Update();
            }
        }

        public void QueueForGeneration(Vector2 pos, bool loadFromDisk)
        {
            if (_chunkTaskManagers[ChunkTaskManager.Types.Generate].Pending(pos))
                return;
            if (_chunkTaskManagers[ChunkTaskManager.Types.Load].Pending(pos)) // chunk is already being loaded or queued for loading
                return;
            if (_chunkTaskManagers[ChunkTaskManager.Types.Save].Pending(pos)) // chunk is being saved or queued for saving
            {
                // if it was queued we have a chance to remove it so that we don't take the time to save before loading
                _chunkTaskManagers[ChunkTaskManager.Types.Save].Cancel(pos);
            }

            if (!GlobalDebugConfig.StaticGlobalConfig.DisableLoad
                && loadFromDisk
                && ChunkHelpers.IsChunkPersisted(pos))
            {
                _chunkTaskManagers[ChunkTaskManager.Types.Load].Enqueue(pos);
            }
            else
            {
                _chunkTaskManagers[ChunkTaskManager.Types.Generate].Enqueue(pos);
            }
        }

        public void QueueForSaving(Chunk chunk)
        {
            _chunkTaskManagers[ChunkTaskManager.Types.Save].Enqueue(chunk);
        }

        public void CancelGeneration()
        {
            _chunkTaskManagers[ChunkTaskManager.Types.Generate].CancelAll();
        }

        public void CancelLoading()
        {
            _chunkTaskManagers[ChunkTaskManager.Types.Load].CancelAll();
        }

        public void ForceSaving()
        {
            _chunkTaskManagers[ChunkTaskManager.Types.Save].CompleteAll();
        }
    }
}