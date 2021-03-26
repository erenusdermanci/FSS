namespace Chunks
{
    public class ChunkNeighborhood<T> where T : Chunk
    {
        protected T[] _chunks;

        protected const int CentralChunkIndex = 4;

        public ChunkNeighborhood(ChunkMap<T> chunkMap, T centralChunk)
        {
            UpdateNeighbors(chunkMap, centralChunk);
        }

        public void UpdateNeighbors(ChunkMap<T> chunkMap, T centralChunk)
        {
            // 6 7 8
            // 4 0 5
            // 1 2 3

            _chunks = new[]
            {
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 0, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, 0),
                centralChunk,
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, 0),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, 1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 0, 1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, 1)
            };
        }

        public T GetCentralChunk()
        {
            return _chunks[CentralChunkIndex];
        }

        public T[] GetChunks()
        {
            return _chunks;
        }
    }
}
