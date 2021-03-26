using System;
using System.Collections.Generic;
using DebugTools;
using Utils;

namespace Chunks.Tasks
{
    public class ChunkTaskManager<T> where T : Chunk, new()
    {
        private readonly int _maximumProcessing;
        private readonly Dictionary<Vector2i, ChunkTask<T>> _tasks = new Dictionary<Vector2i, ChunkTask<T>>();
        private readonly List<Vector2i> _queued = new List<Vector2i>();
        private readonly Dictionary<Vector2i, ChunkTask<T>> _processing = new Dictionary<Vector2i, ChunkTask<T>>();

        private readonly List<ChunkTask<T>> _processed = new List<ChunkTask<T>>();

        public event EventHandler Processed;

        private readonly Func<T, ChunkTask<T>> _taskCreator;

        public ChunkTaskManager(int maximumProcessing, Func<T, ChunkTask<T>> taskCreator)
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
            Enqueue(new T { Position = position });
        }

        public void Enqueue(T chunk)
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

        private void OnProcessed(ChunkTask<T> task)
        {
            _tasks.Remove(task.Chunk.Position);
            _processing.Remove(task.Chunk.Position);
            task.Dispose();
            Processed?.Invoke(this, new ChunkTaskEvent<T>(task.Chunk));
        }
    }
}
