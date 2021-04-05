using System.Collections.Generic;
using Chunks;
using Utils;

namespace Tiles.Tasks
{
    public sealed class TileTaskScheduler
    {
        private readonly Dictionary<TileTaskTypes, TileTaskManager> _tileTaskManagers;

        public TileTaskScheduler(ChunkLayer[] chunkLayers)
        {
            _tileTaskManagers = new Dictionary<TileTaskTypes, TileTaskManager>
            {
                {
                    TileTaskTypes.Save,
                    new TileTaskManager(16, tile => new SaveTask(tile, chunkLayers))
                },
                {
                    TileTaskTypes.Load,
                    new TileTaskManager(16, tile => new LoadTask(tile, chunkLayers))
                }
            };
        }

        public TileTaskManager GetTaskManager(TileTaskTypes tileTaskType)
        {
            return _tileTaskManagers[tileTaskType];
        }

        public void Update()
        {
            foreach (var tileTaskManager in _tileTaskManagers)
            {
                tileTaskManager.Value.Update();
            }
        }

        public void QueueForLoad(Vector2i pos)
        {
            if (_tileTaskManagers[TileTaskTypes.Load].Pending(pos)) // chunk is already being loaded or queued for loading
                return;
            if (_tileTaskManagers[TileTaskTypes.Save].Pending(pos)) // chunk is being saved or queued for saving
            {
                // if it was queued we have a chance to remove it so that we don't take the time to save before loading
                _tileTaskManagers[TileTaskTypes.Save].Cancel(pos);
            }

            _tileTaskManagers[TileTaskTypes.Load].Enqueue(pos);
        }

        public void QueueForSave(Tile tile)
        {
            _tileTaskManagers[TileTaskTypes.Save].Enqueue(tile);
        }

        public void ForceLoad()
        {
            _tileTaskManagers[TileTaskTypes.Load].CompleteAll();
        }

        public void CancelLoad()
        {
            _tileTaskManagers[TileTaskTypes.Load].CancelAll();
        }

        public void ForceSave()
        {
            _tileTaskManagers[TileTaskTypes.Save].CompleteAll();
        }
    }
}
