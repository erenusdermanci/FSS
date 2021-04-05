using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Chunks;
using Chunks.Server;

namespace Tiles.Tasks
{
    public abstract class TileTask : IDisposable
    {
        public bool Done;
        public readonly Tile Tile;
        public List<ChunkServer>[] chunksForMainThread;

        protected readonly string tileFileName;
        protected ChunkLayer[] chunkLayers;

        private bool _synchronous;
        private Task _task;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        protected TileTask(Tile tile, ChunkLayer[] chunkLayers)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Tile = tile;
            tileFileName = $"{TileHelpers.SavePath}\\{tile.TilePosition.x}_{tile.TilePosition.y}";
            this.chunkLayers = chunkLayers;

            if (!Directory.Exists(TileHelpers.SavePath))
                Directory.CreateDirectory(TileHelpers.SavePath);

            chunksForMainThread = new List<ChunkServer>[Tile.LayerCount];
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
