using System;
using System.Collections.Generic;
using Utils;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int HorizontalChunkCount = 4;
        public const int VerticalChunkCount = 3;
        public const int TotalSize = HorizontalChunkCount * VerticalChunkCount;

        // This does not correspond to the world position, but to the relative position in the tilemap
        public Vector2i Position;

        public Tile(Vector2i position)
        {
            Position = position;
        }

        public IEnumerable<Vector2i> GetChunkPositions()
        {
            for (var y = Position.y * VerticalChunkCount; y < Position.y * VerticalChunkCount + VerticalChunkCount; ++y)
            {
                for (var x = Position.x * HorizontalChunkCount; x < Position.x * HorizontalChunkCount + HorizontalChunkCount; ++x)
                {
                    yield return new Vector2i(x, y);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
