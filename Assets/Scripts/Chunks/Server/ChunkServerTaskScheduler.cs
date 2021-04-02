using System;
using System.Collections.Generic;
using Chunks.Tasks;
using DebugTools;
using DebugTools.ProfilingTool;
using Utils;
using static Chunks.ChunkLayer;

namespace Chunks.Server
{
    public sealed class ChunkServerTaskScheduler
    {
        private readonly ChunkLayerType _chunkLayerType;
        private readonly Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>> _chunkTaskManagers;

        public ChunkServerTaskScheduler(ChunkLayerType chunkLayerType)
        {
            _chunkLayerType = chunkLayerType;
            _chunkTaskManagers = new Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>>
            {
                {
                    ChunkTaskTypes.Save,
                    new ChunkTaskManager<ChunkServer>(1, chunk => new SaveTask(chunk, chunkLayerType))
                },
                {
                    ChunkTaskTypes.Load,
                    new ChunkTaskManager<ChunkServer>(16, chunk => new LoadTask(chunk, chunkLayerType))
                },
                {
                    ChunkTaskTypes.Generate,
                    new ChunkTaskManager<ChunkServer>(16, chunk => new GenerationTask(chunk, chunkLayerType))
                }
            };
        }

        public ChunkTaskManager<ChunkServer> GetTaskManager(ChunkTaskTypes chunkTaskType)
        {
            return _chunkTaskManagers[chunkTaskType];
        }

        public void Update()
        {
            foreach (var chunkTaskManager in _chunkTaskManagers)
            {
                UpdateProfilingCounters(chunkTaskManager.Key, chunkTaskManager.Value);
                chunkTaskManager.Value.Update();
            }
        }

        private void UpdateProfilingCounters(ChunkTaskTypes type, ChunkTaskManager<ChunkServer> taskManager)
        {
            switch (type)
            {
                case ChunkTaskTypes.Save:
                {
                    ProfilingTool.SetCounter(ProfilingCounterTypes.ProcessingSaveTasks, taskManager.Processing());
                    ProfilingTool.SetCounter(ProfilingCounterTypes.QueuedSaveTasks, taskManager.Queued());
                    break;
                }
                case ChunkTaskTypes.Load:
                {
                    ProfilingTool.SetCounter(ProfilingCounterTypes.ProcessingLoadTasks, taskManager.Processing());
                    ProfilingTool.SetCounter(ProfilingCounterTypes.QueuedLoadTasks, taskManager.Queued());
                    break;
                }
                case ChunkTaskTypes.Generate:
                {
                    ProfilingTool.SetCounter(ProfilingCounterTypes.ProcessingGenerationTasks, taskManager.Processing());
                    ProfilingTool.SetCounter(ProfilingCounterTypes.QueuedGenerationTasks, taskManager.Queued());
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
                && ChunkHelpers.IsChunkPersisted(_chunkLayerType, pos))
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
