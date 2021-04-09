using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chunks.Tasks
{
    public abstract class ChunkTask<T> : IDisposable  where T : Chunk
    {
        public readonly T Chunk;

        private bool _synchronous;
        private Task _task;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        protected ChunkTask(T chunk)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Chunk = chunk;
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
        }

        protected abstract void Execute();

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
