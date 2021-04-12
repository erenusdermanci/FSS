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

        public static void GetNeighborhoodChunkPositions(Vector2i center, Vector2i[] neighborhoodPositions)
        {
            neighborhoodPositions[0].Set(center.x - 1, center.y - 1);
            neighborhoodPositions[1].Set(center.x, center.y - 1);
            neighborhoodPositions[2].Set(center.x + 1, center.y - 1);
            neighborhoodPositions[3].Set(center.x - 1, center.y);
            neighborhoodPositions[4].Set(center.x, center.y);
            neighborhoodPositions[5].Set(center.x + 1, center.y);
            neighborhoodPositions[6].Set(center.x - 1, center.y + 1);
            neighborhoodPositions[7].Set(center.x, center.y + 1);
            neighborhoodPositions[8].Set(center.x + 1, center.y + 1);
        }
    }
}
