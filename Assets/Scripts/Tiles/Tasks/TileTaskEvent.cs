using System;

namespace Tiles.Tasks
{
    public class TileTaskEvent : EventArgs
    {
        public TileTask Task { get; }

        public TileTaskEvent(TileTask task)
        {
            Task = task;
        }
    }
}
