using System;
using Blocks;
using Utils;

namespace Chunks
{
    public class ChunkNeighborhood
    {
        private readonly Random _rng;

        private Chunk[] Chunks;

        public Chunk this[int idx]
        {
            get => Chunks[idx];
            set => Chunks[idx] = value;
        }

        public ChunkNeighborhood(ChunkMap chunkMap, Chunk centralChunk, Random rng)
        {
            _rng = rng;
            UpdateNeighbors(chunkMap, centralChunk);
        }

        public void UpdateNeighbors(ChunkMap chunkMap, Chunk centralChunk)
        {
            // 6 7 8
            // 4 0 5
            // 1 2 3
            Chunks = new[]
            {
                centralChunk,
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 0, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, -1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, 0),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, 0),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, -1, 1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 0, 1),
                ChunkHelpers.GetNeighborChunk(chunkMap, centralChunk, 1, 1)
            };
        }

        public bool GetBlockInfo(int x, int y, ref Chunk.BlockInfo blockInfo)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
            {
                blockInfo.Type = -1;
                return false;
            }

            Chunks[chunkIndex].GetBlockInfo(y * Chunk.Size + x, ref blockInfo);
            return true;
        }

        public int GetBlockType(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return BlockConstants.Border;

            return Chunks[chunkIndex].GetBlockType(y * Chunk.Size + x);
        }

        public void ReplaceBlock(int x, int y, int type, int stateBitset, float health, float lifetime)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockConstants.BlockDescriptors[type].ColorMaxShift);
            var color = BlockConstants.BlockDescriptors[type].Color;
            Chunks[chunkIndex].PutBlock(x, y, type,
                Helpers.ShiftColorComponent(color.r, shiftAmount),
                Helpers.ShiftColorComponent(color.g, shiftAmount),
                Helpers.ShiftColorComponent(color.b, shiftAmount),
                color.a, stateBitset, health, lifetime);
            UpdateAdjacentBlockDirty(x, y);
        }

        public void UpdateBlock(int x, int y, Chunk.BlockInfo blockInfo, bool resetColor = false)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            if (resetColor)
            {
                var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockConstants.BlockDescriptors[blockInfo.Type].ColorMaxShift);
                var color = BlockConstants.BlockDescriptors[blockInfo.Type].Color;
                Chunks[chunkIndex].PutBlock(x, y, blockInfo.Type,
                    Helpers.ShiftColorComponent(color.r, shiftAmount),
                    Helpers.ShiftColorComponent(color.g, shiftAmount),
                    Helpers.ShiftColorComponent(color.b, shiftAmount),
                    color.a, blockInfo.StateBitset, blockInfo.Health, blockInfo.Lifetime);
            }
            else
            {
                Chunks[chunkIndex].PutBlock(x, y, blockInfo.Type, blockInfo.StateBitset, blockInfo.Health, blockInfo.Lifetime);
            }
            UpdateAdjacentBlockDirty(x, y);
        }

        public void UpdateBlock(int x, int y, Chunk.BlockInfo blockInfo, byte r, byte g, byte b, byte a)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, blockInfo.Type, r, g, b, a, blockInfo.StateBitset, blockInfo.Health, blockInfo.Lifetime);
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
                Chunks[0].Data.colors[srcIndex * 4],
                Chunks[0].Data.colors[srcIndex * 4 + 1],
                Chunks[0].Data.colors[srcIndex * 4 + 2],
                Chunks[0].Data.colors[srcIndex * 4 + 3],
                Chunks[0].Data.stateBitsets[srcIndex],
                Chunks[0].Data.healths[srcIndex],
                Chunks[0].Data.lifetimes[srcIndex]);

            // Chunks[newChunkIndex].SetUpdatedFlag(ux, uy);
            // if (destBlock != BlockConstants.Air)
            //     Chunks[0].SetUpdatedFlag(x, y);

            // put the old destination block at the source position (swap)
            Chunks[0].PutBlock(x, y, destBlock,
                destColorBuffer[0],
                destColorBuffer[1],
                destColorBuffer[2],
                destColorBuffer[3],
                destState,
                destHealth,
                destLifetime);
            UpdateAdjacentBlockDirty(x, y);

            // UpdateDirtyInBorderChunks(x, y);

            return true;
        }

        public void UpdateDirtyRectForAdjacentBlock(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunk);
            if (Chunks[chunk] != null)
            {
                // switch (Chunks[chunk].GetBlockType(y * Chunk.Size + x))
                // {
                //     case BlockConstants.Air:
                //     case BlockConstants.Cloud:
                //     case BlockConstants.Stone:
                //     case BlockConstants.Metal:
                //     case BlockConstants.Border:
                //         break;
                // }
                Chunks[chunk].UpdateBlockDirty(x, y);
            }
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

        private static void UpdateOutsideChunk(ref int x, ref int y, out int chunkIndex)
        {
            chunkIndex = 0;
            if (x < 0 && y < 0)
            {
                // Down Left
                chunkIndex = 1;
                x = Chunk.Size + x;
                y = Chunk.Size + y;
            }
            else if (x >= Chunk.Size && y < 0)
            {
                // Down Right
                chunkIndex = 3;
                x -= Chunk.Size;
                y = Chunk.Size + y;
            }
            else if (y < 0)
            {
                // Down
                chunkIndex = 2;
                y = Chunk.Size + y;
            }
            else if (x < 0 && y >= Chunk.Size)
            {
                // Up Left
                chunkIndex = 6;
                y -= Chunk.Size;
                x = Chunk.Size + x;
            }
            else if (x >= Chunk.Size && y >= Chunk.Size)
            {
                // Up Right
                chunkIndex = 8;
                y -= Chunk.Size;
                x -= Chunk.Size;
            }
            else if (y >= Chunk.Size)
            {
                // Up
                chunkIndex = 7;
                y -= Chunk.Size;
            }
            else if (x < 0)
            {
                // Left
                chunkIndex = 4;
                x = Chunk.Size + x;
            }
            else if (x >= Chunk.Size)
            {
                // Right
                chunkIndex = 5;
                x -= Chunk.Size;
            }
        }
    }
}
