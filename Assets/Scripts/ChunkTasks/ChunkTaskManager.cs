using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DataComponents;
using MonoBehaviours;
using UnityEngine;

namespace ChunkTasks
{
    public class ChunkTaskManager
    {
        public enum Types
        {
            Save,
            Load,
            Generate
        }

        public class ChunkEventArgs : EventArgs
        {
            public Chunk Chunk { get; }

            public ChunkEventArgs(Chunk chunk)
            {
                Chunk = chunk;
            }
        }

        private int MaximumProcessing;
        private readonly ConcurrentDictionary<Vector2, ChunkTask> _tasks = new ConcurrentDictionary<Vector2, ChunkTask>();
        private readonly Queue<ChunkTask> _queued = new Queue<ChunkTask>();
        private readonly Dictionary<Vector2, ChunkTask> _processing = new Dictionary<Vector2, ChunkTask>();

        public event EventHandler Processed;

        private Func<Chunk, ChunkTask> _taskCreator;

        public ChunkTaskManager(int maximumProcessing, Func<Chunk, ChunkTask> taskCreator)
        {
            MaximumProcessing = maximumProcessing;
            _taskCreator = taskCreator;
        }

        public void Update()
        {
            while (_queued.Count != 0 && _processing.Count < MaximumProcessing)
            {
                var task = _queued.Dequeue();
                _processing.Add(task.Chunk.Position, task);
                task.Schedule(GlobalDebugConfig.StaticGlobalConfig.MonothreadSimulate);
            }
        }

        public bool Pending(Vector2 position)
        {
            return _tasks.ContainsKey(position);
        }

        public void Enqueue(Vector2 position)
        {
            if (_tasks.ContainsKey(position))
                return;
            Enqueue(new Chunk { Position = position });
        }

        public void Enqueue(Chunk chunk)
        {
            var task = CreateTask(chunk);
            _queued.Enqueue(task);
            _tasks.TryAdd(chunk.Position, task);

            task.CompleteOnMainThread(t =>
            {
                _tasks.TryRemove(task.Chunk.Position, out var chunkTask);
                OnProcessed(chunkTask.Chunk);
            });
        }

        private ChunkTask CreateTask(Chunk chunk)
        {
            return _taskCreator(chunk);
        }

        public void Complete()
        {
            while (_queued.Count != 0)
            {
                var task = _queued.Dequeue();
                _processing.Add(task.Chunk.Position, task);
                task.Schedule(GlobalDebugConfig.StaticGlobalConfig.MonothreadSimulate);
            }
            foreach (var saveTask in _processing.Values)
            {
                saveTask.Join();
            }
        }

        public void Cancel()
        {
            foreach (var task in _processing.Values)
            {
                task.Cancel();
            }
            
            foreach (var task in _processing.Values)
            {
                task.Join();
            }
            
            _queued.Clear();
            _tasks.Clear();
        }

        private void OnProcessed(Chunk chunk)
        {
            _processing.Remove(chunk.Position);
            Processed?.Invoke(this, new ChunkEventArgs(chunk));
        }
    }
}