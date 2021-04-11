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

        public static IEnumerable<Vector2i> GetTilePositionsAroundCentralTilePosition(Vector2i position, int gridThickness)
        {
            for (var y = -gridThickness; y <= gridThickness; y++)
            {
                for (var x = -gridThickness; x <= gridThickness; x++)
                {
                    yield return new Vector2i(position.x + x, position.y + y);
                }
            }
        }

        public static Vector2i GetTilePositionFromChunkPosition(Vector2i position)
        {
            return new Vector2i(
                (int) Mathf.Floor((float) position.x / Tile.HorizontalSize),
                (int) Mathf.Floor((float) position.y / Tile.VerticalSize)
            );
        }

        public static Vector2i GetTilePositionFromWorldPosition(Vector2 position)
        {
            return new Vector2i(
                (int) Mathf.Floor((position.x + 0.5f) / Tile.HorizontalSize),
                (int) Mathf.Floor((position.y + 0.5f) / Tile.VerticalSize)
            );
        }
    }
}
