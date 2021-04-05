﻿using System.Collections.Generic;
using Utils;

namespace Tiles.Tasks
{
    public class TileDistanceToPlayerComparer : IComparer<Vector2i>
    {
        public int Compare(Vector2i x, Vector2i y)
        {
            var scaledX = new Vector2i(x.x * Tile.HorizontalSize, x.y * Tile.VerticalSize);
            var scaledY = new Vector2i(y.x * Tile.HorizontalSize, y.y * Tile.VerticalSize);
            var cameraPosition = WorldManager.MainCameraPosition;
            return -Vector2i.Distance(scaledX, cameraPosition.x, cameraPosition.y)
                .CompareTo(Vector2i.Distance(scaledY, cameraPosition.x, cameraPosition.y));
        }
    }
}
