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

        public static IEnumerable<Vector2i> GetRelativeTilePositions(int gridThickness)
        {
            for (var y = 0; y < gridThickness * 2 + 1; y++)
            {
                for (var x = 0; x < gridThickness * 2 + 1; x++)
                {
                    yield return new Vector2i(x, y);
                }
            }
        }

        public static Vector2i GetTilePositionFromChunkPosition(Vector2i position)
        {
            return new Vector2i(
                (int) Mathf.Floor((float) position.x / Tile.HorizontalChunks),
                (int) Mathf.Floor((float) position.y / Tile.VerticalChunks)
            );
        }

        public static Vector2i GetTilePositionFromWorldPosition(Vector2 position)
        {
            return new Vector2i(
                (int) Mathf.Floor((position.x + 0.5f) / Tile.HorizontalChunks),
                (int) Mathf.Floor((position.y + 0.5f) / Tile.VerticalChunks)
            );
        }
    }
}
