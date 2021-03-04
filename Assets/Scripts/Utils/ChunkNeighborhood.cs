using DataComponents;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static Constants;

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

        public ChunkNeighborhood(ChunkGrid grid, Chunk centralChunk)
        {
            Chunks = new[]
            {
                centralChunk,
                GetNeighborChunks(grid, centralChunk, -1, -1),
                GetNeighborChunks(grid, centralChunk, 0, -1),
                GetNeighborChunks(grid, centralChunk, 1, -1),
                GetNeighborChunks(grid, centralChunk, -1, 0),
                GetNeighborChunks(grid, centralChunk, 1, 0),
                GetNeighborChunks(grid, centralChunk, -1, 1),
                GetNeighborChunks(grid, centralChunk, 0, 1),
                GetNeighborChunks(grid, centralChunk, 1, 1)
            };
        }

        public int GetBlock(int x, int y, bool current = false)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return (int)Blocks.Border;

            var blockIndex = y * Chunk.Size + x;

            var blockCooldown = Chunks[chunkIndex].BlockUpdateCooldowns[blockIndex];
            if (blockCooldown > 0)
            {
                if (current)
                {
                    Chunks[chunkIndex].BlockUpdateCooldowns[blockIndex]--;
                }

                return CooldownBlockValue;
            }

            return Chunks[chunkIndex].BlockTypes[blockIndex];
        }

        public void PutBlock(int x, int y, int type)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, type);
            Chunks[chunkIndex].Dirty = true;
        }

        public bool MoveBlock(int x, int y, int xOffset, int yOffset, int srcBlock, int destBlock, ref BlockMoveInfo blockMoveInfo)
        {
            // compute the new coordinates and chunk index if we go outside of the current chunk
            var ux = x + xOffset;
            var uy = y + yOffset;
            UpdateOutsideChunk(ref ux, ref uy, out var newChunkIndex);

            // if there is no chunk at the destination, the block cannot move there
            if (Chunks[newChunkIndex] == null)
                return false;

            // put the source block at its destination
            Chunks[newChunkIndex].PutBlock(ux, uy, srcBlock);
            blockMoveInfo.Chunk = newChunkIndex;
            blockMoveInfo.X = ux;
            blockMoveInfo.Y = uy;

            // if we did not put the block in the same chunk,
            // we need to add a cooldown of 1 update,
            // to prevent the other chunk to update it a second time in the same update round
            if (destBlock != (int) Blocks.Air)
                Chunks[newChunkIndex].SetCooldown(ux, uy, 1);

            // put the old destination block at the source position (swap)
            Chunks[0].PutBlock(x, y, destBlock);
            
            UpdateDirtyInBorderChunks(x, y);

            return true;
        }

        private void UpdateDirtyInBorderChunks(int x, int y)
        {
            if (y == Chunk.Size - 1)
                DoUpdateDirtyInBorderChunks(x, y + 1);
            if (y == 0)
                DoUpdateDirtyInBorderChunks(x, y - 1);
            if (x == Chunk.Size - 1)
                DoUpdateDirtyInBorderChunks(x + 1, y);
            if (x == 0)
                DoUpdateDirtyInBorderChunks(x - 1, y);
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

        private static Chunk GetNeighborChunks(ChunkGrid grid, Chunk origin, int xOffset, int yOffset)
        {
            var neighborPosition = new Vector2(origin.Position.x + xOffset, origin.Position.y + yOffset);
            if (grid.ChunkMap.ContainsKey(neighborPosition))
            {
                return grid.ChunkMap[neighborPosition];
            }
            return null;
        }
    }
}
