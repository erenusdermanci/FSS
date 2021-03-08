using System;
using System.Threading;
using System.Threading.Tasks;
using DataComponents;
using UnityEngine;

namespace ChunkTasks
{
    public abstract class ChunkTask : IDisposable
    {
        public bool Done;
        
        public readonly Chunk Chunk;

        private bool _synchronous;
        private Task _task;

        private Task _mainThreadContinuationTask;
        private Action<Task> _mainThreadContinuation;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

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
            _cancellationToken = _cancellationTokenSource.Token;
            _task = new Task(Run, _cancellationToken);
            if (_mainThreadContinuation != null)
                _mainThreadContinuationTask = _task.ContinueWith(_mainThreadContinuation, TaskScheduler.FromCurrentSynchronizationContext());
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