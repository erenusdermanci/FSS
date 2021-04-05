using System;
using Utils;

namespace Tiles
{
    public class Tile : IDisposable
    {
        public const int HorizontalSize = 4;
        public const int VerticalSize = 3;
        public const int LayerCount = 2;

        public Vector2i TilePosition;

        public Tile(Vector2i position)
        {
            TilePosition = position;
        }

        public void Dispose()
        {

        }
    }
}
