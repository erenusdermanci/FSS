using System;
using System.Threading;
using System.Threading.Tasks;
using DataComponents;

namespace ChunkTasks
{
    public abstract class ChunkTask
    {
        public readonly Chunk Chunk;

        private bool _synchronous;
        private Task _task;

        private Action<Task> _mainThreadContinuation;

        private readonly CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken CancellationToken;

        protected ChunkTask(Chunk chunk)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Chunk = chunk;
        }

        public void CompleteOnMainThread(Action<Task> action)
        {
            _mainThreadContinuation = action;
        }
        
        public void Schedule(bool synchronous = false)
        {
            CancellationToken = _cancellationTokenSource.Token;
            _task = new Task(Run, CancellationToken);
            if (_mainThreadContinuation != null)
                _task.ContinueWith(_mainThreadContinuation, TaskScheduler.FromCurrentSynchronizationContext());
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
            _task.Wait(CancellationToken);
            _task = null;
        }

        private void Run()
        {
            Execute();
        }

        protected abstract void Execute();
    }
}