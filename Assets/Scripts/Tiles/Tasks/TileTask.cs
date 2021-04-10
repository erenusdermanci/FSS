using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serialized;

namespace Tiles.Tasks
{
    public abstract class TileTask : IDisposable
    {
        public bool Done;
        public readonly Tile Tile;
        public TileData? TileData;

        protected readonly string TileFileName;
        protected string TileFullFileName;

        private bool _synchronous;
        private Task _task;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        protected TileTask(Tile tile)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Tile = tile;
            TileFileName = $"{tile.Position.x}_{tile.Position.y}";
            TileFullFileName = $"{TileHelpers.TilesSavePath}\\{TileFileName}";

            if (!Directory.Exists(TileHelpers.TilesSavePath))
                Directory.CreateDirectory(TileHelpers.TilesSavePath);
        }

        public void Schedule(bool synchronous = false)
        {
            _cancellationToken = _cancellationTokenSource.Token;
            _task = new Task(Run, _cancellationToken);
            _synchronous = synchronous;
            if (_synchronous)
            {
                _task.RunSynchronously();
            }
            else
            {
                _task.Start();
            }
        }

        public bool Scheduled()
        {
            return _task != null;
        }

        public bool Queued()
        {
            return _task.Status <= TaskStatus.Created;
        }

        public void Cancel()
        {
            if (_task == null)
                return;
            _cancellationTokenSource.Cancel();
        }

        public void Join()
        {
            if (_synchronous)
                return;
            _task.Wait(_cancellationToken);
            _task = null;
        }

        private void Run()
        {
            Execute();

            Done = true;
        }

        protected abstract void Execute();

        protected bool ShouldCancel()
        {
            if (!_cancellationToken.IsCancellationRequested)
                return false;

            Done = true;
            return true;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
