using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using Utils;

namespace Tiles
{
    public class TileMap
    {
        public readonly Dictionary<Vector2i, Tile> Map = new Dictionary<Vector2i, Tile>();

        [CanBeNull]
        public Tile this[Vector2i i] => Map.ContainsKey(i) ? Map[i] : null;

        public void Clear()
        {
            Map.Clear();
        }

        public bool Contains(Vector2i position)
        {
            return Map.ContainsKey(position);
        }

        public void Add(Tile tile)
        {
            Map.Add(tile.Position, tile);
        }

        public void Remove(Vector2i position)
        {
            Map.Remove(position);
        }

        public IEnumerable<Tile> Tiles()
        {
            return Map.Values;
        }
    }
}
