using System.Collections.Concurrent;
using DataComponents;
using UnityEngine;
using static BlockConstants;

namespace Utils
{
    public class ChunkNeighborhood
    {
        public struct BlockMoveInfo
        {
            public int Chunk;
            public int X;
            public int Y;
        }
    
        public Chunk[] Chunks;

        public Chunk this[int idx]
        {
            get => Chunks[idx];
            set => Chunks[idx] = value;
        }

        public ChunkNeighborhood(ConcurrentDictionary<Vector2, Chunk> chunkMap, Chunk centralChunk)
        {
            UpdateNeighbors(chunkMap, centralChunk);
        }

        public void UpdateNeighbors(ConcurrentDictionary<Vector2, Chunk> chunkMap, Chunk centralChunk)
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

        public int GetBlock(int x, int y, bool current = false)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return (int)Blocks.Border;

            var blockIndex = y * Chunk.Size + x;

            return Chunks[chunkIndex].Data.types[blockIndex];
        }

        public void PutBlock(int x, int y, int type, Color32 color, ref Chunk chunkWritten)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, type, color.r, color.g, color.b, color.a);
            Chunks[chunkIndex].Dirty = true;
            chunkWritten = Chunks[chunkIndex];
        }

        public unsafe bool MoveBlock(int x, int y, int xOffset, int yOffset, int srcBlock, int destBlock, ref BlockMoveInfo blockMoveInfo)
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

            Chunks[newChunkIndex].PutBlock(ux, uy, srcBlock,
                Chunks[0].Data.colors[srcIndex * 4],
                Chunks[0].Data.colors[srcIndex * 4 + 1],
                Chunks[0].Data.colors[srcIndex * 4 + 2],
                Chunks[0].Data.colors[srcIndex * 4 + 3]);
            blockMoveInfo.Chunk = newChunkIndex;
            blockMoveInfo.X = ux;
            blockMoveInfo.Y = uy;

            Chunks[newChunkIndex].SetUpdatedFlag(ux, uy);
            if (destBlock != (int)Blocks.Air)
                Chunks[0].SetUpdatedFlag(x, y);

            // put the old destination block at the source position (swap)
            Chunks[0].PutBlock(x, y, destBlock,
                destColorBuffer[0],
                destColorBuffer[1],
                destColorBuffer[2],
                destColorBuffer[3]);
            
            UpdateDirtyInBorderChunks(x, y);

            return true;
        }

        private void UpdateDirtyInBorderChunks(int x, int y)
        {
            switch (y)
            {
                case Chunk.Size - 1:
                    DoUpdateDirtyInBorderChunks(x, y + 1);
                    break;
                case 0:
                    DoUpdateDirtyInBorderChunks(x, y - 1);
                    break;
            }

            switch (x)
            {
                case Chunk.Size - 1:
                    DoUpdateDirtyInBorderChunks(x + 1, y);
                    break;
                case 0:
                    DoUpdateDirtyInBorderChunks(x - 1, y);
                    break;
            }
        }

        private void DoUpdateDirtyInBorderChunks(int xOffset, int yOffset)
        {
            UpdateOutsideChunk(ref xOffset, ref yOffset, out var aboveChunkIndex);
            if (Chunks[aboveChunkIndex] != null)
                Chunks[aboveChunkIndex].Dirty = true;
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
