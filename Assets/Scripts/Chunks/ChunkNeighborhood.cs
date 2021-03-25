using Blocks;

namespace Chunks
{
    public class ChunkNeighborhood
    {
        private ChunkServer[] _chunks;

        private const int CentralChunkIndex = 4;

        public ChunkNeighborhood(ChunkMap<ChunkServer> chunkMap, ChunkServer centralChunk)
        {
            UpdateNeighbors(chunkMap, centralChunk);
        }

        public void UpdateNeighbors(ChunkMap<ChunkServer> chunkMap, ChunkServer centralChunk)
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

        public ChunkServer GetCentralChunk()
        {
            return _chunks[CentralChunkIndex];
        }

        public bool GetBlockInfo(int x, int y, ref Block block)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (_chunks[chunkIndex] == null)
            {
                block.Type = -1;
                return false;
            }

            _chunks[chunkIndex].GetBlockInfo(y * Chunk.Size + x, ref block);
            return true;
        }

        public int GetBlockType(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (_chunks[chunkIndex] == null)
                return BlockConstants.Border;

            return _chunks[chunkIndex].GetBlockType(y * Chunk.Size + x);
        }

        public int GetBlockUpdatedFlag(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (_chunks[chunkIndex] == null)
                return ChunkManager.UpdatedFlag;

            return _chunks[chunkIndex].BlockUpdatedFlags[y * Chunk.Size + x];
        }

        public void ReplaceBlock(int x, int y, int type, int stateBitset, float health, float lifetime)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (_chunks[chunkIndex] == null)
                return;
            var color = BlockConstants.BlockDescriptors[type].Color;
            color.Shift(out var r, out var g, out var b);
            _chunks[chunkIndex].PutBlock(x, y, type, r, g, b,
                color.a, stateBitset, health, lifetime);
            UpdateAdjacentBlockDirty(x, y);
        }

        public void UpdateBlock(int x, int y, Block block, byte r, byte g, byte b, byte a)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (_chunks[chunkIndex] == null)
                return;
            _chunks[chunkIndex].PutBlock(x, y, block.Type, r, g, b, a, block.StateBitset, block.Health, block.Lifetime);
            UpdateAdjacentBlockDirty(x, y);
        }

        public unsafe bool MoveBlock(int x, int y, int xOffset, int yOffset, int srcBlock, int destBlock)
        {
            // compute the new coordinates and chunk index if we go outside of the current chunk
            var ux = x + xOffset;
            var uy = y + yOffset;
            UpdateOutsideChunk(ref ux, ref uy, out var newChunkIndex);

            // if there is no chunk at the destination, the block cannot move there
            if (_chunks[newChunkIndex] == null)
                return false;

            // put the source block at its destination
            // handle color swapping
            var srcIndex = Chunk.Size * y + x;
            var dstIndex = Chunk.Size * uy + ux;
            var destColorBuffer = stackalloc byte[4] {
                _chunks[newChunkIndex].Data.colors[dstIndex * 4],
                _chunks[newChunkIndex].Data.colors[dstIndex * 4 + 1],
                _chunks[newChunkIndex].Data.colors[dstIndex * 4 + 2],
                _chunks[newChunkIndex].Data.colors[dstIndex * 4 + 3]
            };
            var destState = _chunks[newChunkIndex].Data.stateBitsets[dstIndex];
            var destHealth = _chunks[newChunkIndex].Data.healths[dstIndex];
            var destLifetime = _chunks[newChunkIndex].Data.lifetimes[dstIndex];

            _chunks[newChunkIndex].PutBlock(ux, uy, srcBlock,
                _chunks[CentralChunkIndex].Data.colors[srcIndex * 4],
                _chunks[CentralChunkIndex].Data.colors[srcIndex * 4 + 1],
                _chunks[CentralChunkIndex].Data.colors[srcIndex * 4 + 2],
                _chunks[CentralChunkIndex].Data.colors[srcIndex * 4 + 3],
                _chunks[CentralChunkIndex].Data.stateBitsets[srcIndex],
                _chunks[CentralChunkIndex].Data.healths[srcIndex],
                _chunks[CentralChunkIndex].Data.lifetimes[srcIndex]);

            // put the old destination block at the source position (swap)
            _chunks[CentralChunkIndex].PutBlock(x, y, destBlock,
                destColorBuffer[0],
                destColorBuffer[1],
                destColorBuffer[2],
                destColorBuffer[3],
                destState,
                destHealth,
                destLifetime);
            UpdateAdjacentBlockDirty(x, y);

            return true;
        }

        public void UpdateAdjacentBlockDirty(int x, int y)
        {
            UpdateDirtyRectForAdjacentBlock(x - 1, y - 1);
            UpdateDirtyRectForAdjacentBlock(x, y - 1);
            UpdateDirtyRectForAdjacentBlock(x + 1, y - 1);
            UpdateDirtyRectForAdjacentBlock(x - 1, y);
            UpdateDirtyRectForAdjacentBlock(x + 1, y);
            UpdateDirtyRectForAdjacentBlock(x - 1, y + 1);
            UpdateDirtyRectForAdjacentBlock(x, y + 1);
            UpdateDirtyRectForAdjacentBlock(x + 1, y + 1);
        }

        public void UpdateDirtyRectForAdjacentBlock(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunk);
            if (_chunks[chunk] != null)
            {
                _chunks[chunk].UpdateBlockDirty(x, y);
            }
        }

        public static void UpdateOutsideChunk(ref int x, ref int y, out int chunkIndex)
        {
            var ix = (int) (x / (float)Chunk.Size + 32768.0f) - 32768;
            var iy = (int) (y / (float)Chunk.Size + 32768.0f) - 32768;
            chunkIndex = (iy + 1) * 3 + ix + 1;
            x += -ix * Chunk.Size;
            y += -iy * Chunk.Size;
        }
    }
}
