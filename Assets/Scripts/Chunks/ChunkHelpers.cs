using Utils;

namespace Chunks
{
    public static class ChunkHelpers
    {
        public static T GetNeighborChunk<T>(ChunkMap<T> chunkMap, T origin, int xOffset, int yOffset) where T : Chunk
        {
            var neighborPosition = new Vector2i(origin.Position.x + xOffset, origin.Position.y + yOffset);
            return chunkMap.Contains(neighborPosition) ? chunkMap[neighborPosition] : null;
        }
    }
}
