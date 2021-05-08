using System.Collections.Generic;
using Chunks;
using Tools;
using UnityEngine;
using Utils;

namespace Tiles
{
    public class Tile
    {
        public const int HorizontalChunks = 4;
        public const int VerticalChunks = 4;
        public const int ChunkAmount = HorizontalChunks * VerticalChunks;

        public readonly WorldManager WorldManager;

        private readonly Chunk[] _chunks;

        // This does not correspond to the world position, but to the relative position in the tilemap
        public Vector2i Position;

        public Tile(WorldManager worldManager, Vector2i position)
        {
            WorldManager = worldManager;
            Position = position;
            _chunks = new Chunk[ChunkAmount];

            var i = 0;
            for (var y = 0; y < VerticalChunks; ++y)
            {
                for (var x = 0; x < HorizontalChunks; ++x)
                {
                    _chunks[i] = new Chunk(this, x, y);
                    i++;
                }
            }

        }

        public void Update()
        {
            foreach (var chunk in _chunks)
            {
                chunk.Update();
            }
        }
    }
}
