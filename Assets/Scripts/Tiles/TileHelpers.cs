using System.Collections.Generic;
using UnityEngine;
using Utils;
using static Utils.UnityHelpers.Helpers;

namespace Tiles
{
    public static class TileHelpers
    {
        public static readonly string TilesSavePath = $"{SavePath()}\\Tiles";

        public static IEnumerable<Vector2i> GetTilePositionsAroundCentralTilePosition(Vector2i pos)
        {
            var positions = new List<Vector2i>();

            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    positions.Add(new Vector2i(pos.x + x, pos.y + y));
                }
            }
            return positions;
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
