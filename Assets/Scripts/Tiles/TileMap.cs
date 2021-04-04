using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using Utils;

namespace Tiles
{
    public class TileMap
    {
        public readonly ConcurrentDictionary<Vector2i, Tile> Map = new ConcurrentDictionary<Vector2i, Tile>();

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
            Map.TryAdd(tile.TilePosition, tile);
        }

        public void Remove(Vector2i position)
        {
            Map.TryRemove(position, out _);
        }

        public IEnumerable<Tile> Tiles()
        {
            return Map.Values;
        }
    }
}
