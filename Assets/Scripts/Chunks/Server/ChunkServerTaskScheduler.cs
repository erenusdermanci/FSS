using System;
using System.Collections.Generic;
using Chunks.Tasks;
using Tools.ProfilingTool;
using Utils;
using static Chunks.ChunkLayer;

namespace Chunks.Server
{
    public sealed class ChunkServerTaskScheduler
    {
        private readonly Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>> _chunkTaskManagers;

        public ChunkServerTaskScheduler(ChunkLayerType chunkLayerType)
        {
            _chunkTaskManagers = new Dictionary<ChunkTaskTypes, ChunkTaskManager<ChunkServer>>
            {
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

        public void QueueForGeneration(Vector2i pos)
        {
            if (_chunkTaskManagers[ChunkTaskTypes.Generate].Pending(pos))
                return;

            _chunkTaskManagers[ChunkTaskTypes.Generate].Enqueue(pos);
        }

        public void CancelGeneration()
        {
            _chunkTaskManagers[ChunkTaskTypes.Generate].CancelAll();
        }
    }
}
