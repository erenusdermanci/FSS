using System.Collections.Generic;
using UnityEngine;

namespace Chunks.Tasks
{
    public class ChunkDistanceToPlayerComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 x, Vector2 y)
        {
            var playerPosition = ChunkManager.PlayerPosition;
            return -Vector2.Distance(x, playerPosition)
                .CompareTo(Vector2.Distance(y, playerPosition));
        }
    }
}