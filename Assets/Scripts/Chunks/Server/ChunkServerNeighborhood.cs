﻿using System.Runtime.CompilerServices;
using Blocks;
using Serialized;

namespace Chunks.Server
{
    public class ChunkServerNeighborhood : ChunkNeighborhood<ChunkServer>
    {
        public ChunkServerNeighborhood(ChunkMap<ChunkServer> chunkMap, ChunkServer centralChunk)
            : base(chunkMap, centralChunk)
        {

        }

        public bool GetBlockInfo(int x, int y, ref Block block)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
            {
                block.Type = -1;
                return false;
            }

            Chunks[chunkIndex].GetBlockInfo(y * Chunk.Size + x, ref block);
            return true;
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
                return ChunkManager.UpdatedFlag;

            return Chunks[chunkIndex].BlockUpdatedFlags[y * Chunk.Size + x];
        }

        public ref PlantBlockData GetPlantBlockData(int x, int y, int type)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            return ref Chunks[chunkIndex].GetPlantBlockData(x, y, type);
        }

        public void ReplaceBlock(int x, int y, int type, int stateBitset, float health, float lifetime)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            var color = BlockConstants.BlockDescriptors[type].Color;
            color.Shift(out var r, out var g, out var b);
            Chunks[chunkIndex].PutBlock(x, y, type, r, g, b,
                color.a, stateBitset, health, lifetime);
            UpdateAdjacentBlockDirty(x, y);
        }

        public void UpdateBlock(int x, int y, Block block, byte r, byte g, byte b, byte a)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, block.Type, r, g, b, a, block.StateBitset, block.Health, block.Lifetime);
            UpdateAdjacentBlockDirty(x, y);
        }

        public unsafe bool MoveBlock(int x, int y, int xOffset, int yOffset, int srcBlock, int destBlock)
        {
            // compute the new coordinates and chunk index if we go outside of the current chunk
            var ux = x + xOffset;
            var uy = y + yOffset;
            UpdateOutsideChunk(ref ux, ref uy, out var newChunkIndex);

            // if there is no chunk at the destination, the block cannot move there
            if (Chunks[newChunkIndex] == null)
                return false;

            // put the source block at its destination
            // handle color swapping
            var srcIndex = Chunk.Size * y + x;
            var dstIndex = Chunk.Size * uy + ux;
            var destColorBuffer = stackalloc byte[4] {
                Chunks[newChunkIndex].Data.colors[dstIndex * 4],
                Chunks[newChunkIndex].Data.colors[dstIndex * 4 + 1],
                Chunks[newChunkIndex].Data.colors[dstIndex * 4 + 2],
                Chunks[newChunkIndex].Data.colors[dstIndex * 4 + 3]
            };
            var destState = Chunks[newChunkIndex].Data.stateBitsets[dstIndex];
            var destHealth = Chunks[newChunkIndex].Data.healths[dstIndex];
            var destLifetime = Chunks[newChunkIndex].Data.lifetimes[dstIndex];

            Chunks[newChunkIndex].PutBlock(ux, uy, srcBlock,
                Chunks[4].Data.colors[srcIndex * 4],
                Chunks[4].Data.colors[srcIndex * 4 + 1],
                Chunks[4].Data.colors[srcIndex * 4 + 2],
                Chunks[4].Data.colors[srcIndex * 4 + 3],
                Chunks[4].Data.stateBitsets[srcIndex],
                Chunks[4].Data.healths[srcIndex],
                Chunks[4].Data.lifetimes[srcIndex]);

            // put the old destination block at the source position (swap)
            Chunks[4].PutBlock(x, y, destBlock,
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
            if (Chunks[chunk] != null)
            {
                Chunks[chunk].UpdateBlockDirty(x, y);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void UpdateOutsideChunk(ref int x, ref int y, out int chunkIndex)
        {
            var ix = (int) (x / 64.0f + 32768.0f) - 32768;
            var iy = (int) (y / 64.0f + 32768.0f) - 32768;
            chunkIndex = (iy + 1) * 3 + ix + 1;
            x += -ix * Chunk.Size;
            y += -iy * Chunk.Size;
        }
    }
}
