using System.Runtime.CompilerServices;
using Blocks;
using Serialized;
using Tiles;

namespace Chunks.Server
{
    public class ChunkServerNeighborhood : ChunkNeighborhood<ChunkServer>
    {
        public ChunkServerNeighborhood(ChunkMap<ChunkServer> chunkMap, ChunkServer centralChunk)
            : base(chunkMap, centralChunk)
        {
        }

        public int GetBlockType(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return BlockConstants.Border;

            return Chunks[chunkIndex].GetBlockType(y * Chunk.Size + x);
        }

        public int GetBlockUpdatedFlag(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return WorldManager.UpdatedFlag;

            return Chunks[chunkIndex].BlockUpdatedFlags[y * Chunk.Size + x];
        }

        public ref PlantBlockData GetPlantBlockData(int x, int y, int type)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            return ref Chunks[chunkIndex].GetPlantBlockData(x, y, type);
        }

        public void ReplaceBlock(int x, int y, int type, int stateBitset, float health, float lifetime, long entityId)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            var color = BlockConstants.BlockDescriptors[type].Color;
            color.Shift(out var r, out var g, out var b);
            Chunks[chunkIndex].PutBlock(x, y, type, r, g, b,
                color.a, stateBitset, health, lifetime, entityId);
            UpdateAdjacentBlockDirty(x, y);
        }

        public unsafe void UpdateBlock(int x, int y, Block* block, byte r, byte g, byte b, byte a)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, block->type, r, g, b, a, block->states, block->health, block->lifetime, block->entityId);
            UpdateAdjacentBlockDirty(x, y);
        }

        public unsafe bool MoveBlock(int x, int y, int xOffset, int yOffset, int srcBlockType, int destBlockType)
        {
            // compute the new coordinates and chunk index if we go outside of the current chunk
            var ux = x + xOffset;
            var uy = y + yOffset;
            UpdateOutsideChunk(ref ux, ref uy, out var newChunkIndex);

            // if there is no chunk at the destination, the block cannot move there
            var newChunk = Chunks[newChunkIndex];
            if (newChunk == null)
                return false;

            // put the source block at its destination
            // handle color swapping
            var srcIndex = Chunk.Size * y + x;
            var dstIndex = Chunk.Size * uy + ux;
            var destColorBuffer = stackalloc byte[4] {
                newChunk.Colors[dstIndex * 4],
                newChunk.Colors[dstIndex * 4 + 1],
                newChunk.Colors[dstIndex * 4 + 2],
                newChunk.Colors[dstIndex * 4 + 3]
            };
            var destBlock = newChunk.Blocks[dstIndex]; // makes a copy of this block

            var centralChunk = Chunks[CentralChunkIndex];
            ref var srcBlock = ref centralChunk.Blocks[srcIndex];
            newChunk.PutBlock(ux, uy, srcBlockType,
                centralChunk.Colors[srcIndex * 4],
                centralChunk.Colors[srcIndex * 4 + 1],
                centralChunk.Colors[srcIndex * 4 + 2],
                centralChunk.Colors[srcIndex * 4 + 3],
                srcBlock.states,
                srcBlock.health,
                srcBlock.lifetime,
                srcBlock.entityId);

            // put the old destination block at the source position (swap)
            centralChunk.PutBlock(x, y, destBlockType,
                destColorBuffer[0],
                destColorBuffer[1],
                destColorBuffer[2],
                destColorBuffer[3],
                destBlock.states,
                destBlock.health,
                destBlock.lifetime,
                destBlock.entityId);
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

        private void UpdateDirtyRectForAdjacentBlock(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            var chunk = Chunks[chunkIndex];
            chunk?.UpdateBlockDirty(x, y, chunk.GetBlockType(y * Chunk.Size + x));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateOutsideChunk(ref int x, ref int y, out int chunkIndex)
        {
            var ix = (int) (x / 64.0f + 32768.0f) - 32768;
            var iy = (int) (y / 64.0f + 32768.0f) - 32768;
            chunkIndex = (iy + 1) * 3 + ix + 1;
            x += -ix * Chunk.Size;
            y += -iy * Chunk.Size;
        }
    }
}
