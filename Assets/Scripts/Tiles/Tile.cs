using System;
using Utils;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int HorizontalSize = 4;
        public const int VerticalSize = 3;
        public const int LayerCount = 2;

        // This does not correspond to the world position, but to the relative position in the tilemap
        public Vector2i Position;

        public Tile(Vector2i position)
        {
            Position = position;
        }

        public void Dispose()
        {

        }
    }
}
