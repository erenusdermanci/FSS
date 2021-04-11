using System.Collections.Generic;
using Utils;

namespace Tiles.Tasks
{
    public sealed class TileTaskScheduler
    {
        private readonly Dictionary<TileTaskTypes, TileTaskManager> _tileTaskManagers;

        public TileTaskScheduler()
        {
            _tileTaskManagers = new Dictionary<TileTaskTypes, TileTaskManager>
            {
                {
                    TileTaskTypes.Save,
                    new TileTaskManager(16, tile => new TileSaveTask(tile))
                },
                {
                    TileTaskTypes.Load,
                    new TileTaskManager(16, tile => new TileLoadTask(tile))
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

        public TileLoadTask QueueForLoad(Vector2i position)
        {
            if (_tileTaskManagers[TileTaskTypes.Load].Pending(position)) // chunk is already being loaded or queued for loading
                return null;
            if (_tileTaskManagers[TileTaskTypes.Save].Pending(position)) // chunk is being saved or queued for saving
            {
                // if it was queued we have a chance to remove it so that we don't take the time to save before loading
                _tileTaskManagers[TileTaskTypes.Save].Cancel(position);
            }

            return (TileLoadTask)_tileTaskManagers[TileTaskTypes.Load].Enqueue(position);
        }

        public TileSaveTask QueueForSave(Tile tile)
        {
            if (_tileTaskManagers[TileTaskTypes.Load].Pending(tile.Position))
            {
                _tileTaskManagers[TileTaskTypes.Load].Cancel(tile.Position);
            }

            return (TileSaveTask)_tileTaskManagers[TileTaskTypes.Save].Enqueue(tile);
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
