using System.Collections.Generic;
using Utils;

namespace Chunks.Tasks
{
    public class ChunkDistanceToPlayerComparer : IComparer<Vector2i>
    {
        public int Compare(Vector2i x, Vector2i y)
        {
            var playerPosition = ChunkManager.PlayerPosition;
            return -Vector2i.Distance(x, playerPosition.x, playerPosition.y)
                .CompareTo(Vector2i.Distance(y, playerPosition.x, playerPosition.y));
        }
    }
}