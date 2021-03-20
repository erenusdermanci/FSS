using System;
using System.Collections.Generic;
using DebugTools;
using Utils;

namespace Chunks.Tasks
{
    public class ChunkTaskManager
    {
        private readonly int _maximumProcessing;
        private readonly Dictionary<Vector2i, ChunkTask> _tasks = new Dictionary<Vector2i, ChunkTask>();
        private readonly List<Vector2i> _queued = new List<Vector2i>();
        private readonly Dictionary<Vector2i, ChunkTask> _processing = new Dictionary<Vector2i, ChunkTask>();

        private readonly List<ChunkTask> _processed = new List<ChunkTask>();

        public event EventHandler Processed;

        private readonly Func<ChunkServer, ChunkTask> _taskCreator;

        public ChunkTaskManager(int maximumProcessing, Func<ChunkServer, ChunkTask> taskCreator)
        {
            _maximumProcessing = maximumProcessing;
            _taskCreator = taskCreator;
        }

        public void Update()
        {
            _queued.Sort(new ChunkDistanceToPlayerComparer());
            while (_queued.Count != 0 && _processing.Count < _maximumProcessing)
            {
                var index = _queued.Count - 1;
                var position = _queued[index];
                _queued.RemoveAt(index);
                if (!_tasks.ContainsKey(position))
                    continue;
                var task = _tasks[position];
                _processing.Add(task.Chunk.Position, task);
                task.Schedule(GlobalDebugConfig.StaticGlobalConfig.monoThreadSimulate);
            }

            ProcessDoneTasks();
        }

        private void ProcessDoneTasks()
        {
            foreach (var processingTask in _processing.Values)
            {
                if (processingTask.Done)
                    _processed.Add(processingTask);
            }

            foreach (var processedTask in _processed)
            {
                OnProcessed(processedTask);
            }
            _processed.Clear();
        }

        public bool Pending(Vector2i position)
        {
            return _tasks.ContainsKey(position);
        }

        public void Enqueue(Vector2i position)
        {
            if (_tasks.ContainsKey(position))
                return;
            Enqueue(new ChunkServer { Position = position });
        }

        public void Enqueue(ChunkServer chunk)
        {
            var task = _taskCreator(chunk);
            _queued.Add(chunk.Position);
            _tasks.Add(chunk.Position, task);
        }

        public void CompleteAll()
        {
            while (_queued.Count != 0)
            {
                var index = _queued.Count - 1;
                var position = _queued[index];
                _queued.RemoveAt(index);
                var task = _tasks[position];
                _processing.Add(task.Chunk.Position, task);
                task.Schedule(GlobalDebugConfig.StaticGlobalConfig.monoThreadSimulate);
            }
            foreach (var saveTask in _processing.Values)
            {
                saveTask.Join();
            }

            ProcessDoneTasks();
        }

        public void Cancel(Vector2i position)
        {
            if (_processing.ContainsKey(position))
            {
                var task = _processing[position];
                task.Cancel();
                task.Join();
                OnProcessed(task);
            }
            else
            {
                _tasks.Remove(position);
            }
        }

        public void CancelAll()
        {
            foreach (var task in _processing.Values)
            {
                task.Cancel();
            }

            foreach (var task in _processing.Values)
            {
                task.Join();
            }

            ProcessDoneTasks();

            _queued.Clear();
            _tasks.Clear();
        }

        private void OnProcessed(ChunkTask task)
        {
            _tasks.Remove(task.Chunk.Position);
            _processing.Remove(task.Chunk.Position);
            task.Dispose();
            Processed?.Invoke(this, new ChunkTaskEvent(task.Chunk));
        }
    }
}