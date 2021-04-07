using System;
using System.Collections.Generic;
using Utils;

namespace Tiles.Tasks
{
    public class TileTaskManager
    {
        private readonly int _maximumProcessing;
        private readonly Dictionary<Vector2i, TileTask> _tasks = new Dictionary<Vector2i, TileTask>();
        private readonly List<Vector2i> _queued = new List<Vector2i>();
        private readonly Dictionary<Vector2i, TileTask> _processing = new Dictionary<Vector2i, TileTask>();
        private readonly List<TileTask> _processed = new List<TileTask>();

        public int Processing() => _processing.Count;
        public int Queued() => _queued.Count;

        public event EventHandler Processed;

        private readonly Func<Tile, TileTask> _taskCreator;

        public TileTaskManager(int maximumProcessing, Func<Tile, TileTask> taskCreator)
        {
            _maximumProcessing = maximumProcessing;
            _taskCreator = taskCreator;
        }

        public void Update()
        {
            _queued.Sort(new TileDistanceToPlayerComparer());
            while (_queued.Count != 0 && _processing.Count < _maximumProcessing)
            {
                var index = _queued.Count - 1;
                var position = _queued[index];
                _queued.RemoveAt(index);
                if (!_tasks.ContainsKey(position))
                    continue;
                var task = _tasks[position];
                _processing.Add(task.Tile.Position, task);
                task.Schedule();
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
            Enqueue(new Tile(position));
        }

        public void Enqueue(Tile tile)
        {
            if (_tasks.ContainsKey(tile.Position))
                return;
            var task = _taskCreator(tile);
            _queued.Add(tile.Position);
            _tasks.Add(tile.Position, task);
        }

        public void CompleteAll()
        {
            while (_queued.Count != 0)
            {
                var index = _queued.Count - 1;
                var position = _queued[index];
                _queued.RemoveAt(index);
                var task = _tasks[position];
                _processing.Add(task.Tile.Position, task);
                task.Schedule();
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

        private void OnProcessed(TileTask task)
        {
            _tasks.Remove(task.Tile.Position);
            _processing.Remove(task.Tile.Position);
            Processed?.Invoke(this, new TileTaskEvent(task));
            task.Dispose();
        }
    }
}
