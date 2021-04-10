using System;
using System.Collections.Generic;
using Utils;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int HorizontalSize = 4;
        public const int VerticalSize = 3;
        public const int TotalSize = HorizontalSize * VerticalSize;

        // This does not correspond to the world position, but to the relative position in the tilemap
        public Vector2i Position;

        public Tile(Vector2i position)
        {
            Position = position;
        }

        public IEnumerable<Vector2i> GetChunkPositions()
        {
            for (var y = Position.y * VerticalSize; y < Position.y * VerticalSize + VerticalSize; ++y)
            {
                for (var x = Position.x * HorizontalSize; x < Position.x * HorizontalSize + HorizontalSize; ++x)
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
