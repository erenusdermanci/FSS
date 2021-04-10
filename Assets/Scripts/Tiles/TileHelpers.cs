using System.Collections.Generic;
using UnityEngine;
using Utils;
using static Utils.UnityHelpers.Helpers;

namespace Tiles
{
    public static class TileHelpers
    {
        public static readonly string TilesInitialLoadPath = $"{InitialLoadPath()}\\Tiles";
        public static readonly string TilesSavePath = $"{SavePath()}\\Tiles";

        public static IEnumerable<Vector2i> GetTilePositionsAroundCentralTilePosition(Vector2i pos, int gridThickness)
        {
            for (var y = -gridThickness; y <= gridThickness; y++)
            {
                for (var x = -gridThickness; x <= gridThickness; x++)
                {
                    yield return new Vector2i(pos.x + x, pos.y + y);
                }
            }
        }

        public static Vector2i GetTilePositionFromFlooredWorldPosition(Vector2i pos)
        {
            int x;
            int y;
            if (pos.x < 0)
            {
                x = (int) Mathf.Floor((float) pos.x / Tile.HorizontalSize);
            }
            else
            {
                x = pos.x / Tile.HorizontalSize;
            }

            if (pos.y < 0)
            {
                y = (int) Mathf.Floor((float) pos.y / Tile.VerticalSize);
            }
            else
            {
                y = pos.y / Tile.VerticalSize;
            }

            return new Vector2i(x, y);
        }
    }
}
