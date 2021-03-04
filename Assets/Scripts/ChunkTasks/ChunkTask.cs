using System;
using System.Threading.Tasks;
using DataComponents;

namespace ChunkTasks
{
    public abstract class ChunkTask
    {
        public readonly Chunk Chunk;

        private bool _synchronous;
        private Task _task;

        protected ChunkTask(Chunk chunk)
        {
            Chunk = chunk;
        }
        
        public void Schedule(bool synchronous = false)
        {
            _synchronous = synchronous;
            _task = new Task(Execute);
            if (_synchronous)
            {
                _task.RunSynchronously();
            }
            else
            {
                _task.Start();
            }
        }

        public void Join()
        {
            if (_synchronous)
                return;
            _task.Wait();
        }

        public void ReloadTexture()
        {
            Chunk.Texture.LoadRawTextureData(Chunk.BlockColors);
            Chunk.Texture.Apply();
        }

        protected abstract void Execute();
    }
}