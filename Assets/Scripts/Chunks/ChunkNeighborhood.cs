using System.Collections.Concurrent;
using Blocks;
using UnityEngine;
using Utils;
using Random = System.Random;

namespace Chunks
{
    public class ChunkNeighborhood
    {
        public struct BlockData
        {
            public int Type;
            public int StateBitset;
            public float Health;
            public float Lifetime;

            public bool GetState(int stateToCheck)
            {
                return ((StateBitset >> stateToCheck) & 1) == 1;
            }

            public void SetState(int stateToSet)
            {
                StateBitset |= 1 << stateToSet;
            }

            public void ClearState(int stateToClear)
            {
                StateBitset &= ~(1 << stateToClear);
            }
        }

        public struct BlockMoveInfo
        {
            public int Chunk;
            public int X;
            public int Y;
        }

        private Random _rng;

        public Chunk[] Chunks;

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

        public bool GetBlockData(int x, int y, ref BlockData blockData)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
            {
                blockData.Type = -1;
                return false;
            }

            var chunkData = Chunks[chunkIndex].Data;
            var blockIndex = y * Chunk.Size + x;
            blockData.Type = chunkData.types[blockIndex];
            blockData.StateBitset = chunkData.stateBitsets[blockIndex];
            blockData.Health = chunkData.healths[blockIndex];
            blockData.Lifetime = chunkData.lifetimes[blockIndex];
            return true;
        }

        public bool SetBlockStates(int x, int y, int states)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return false;

            Chunks[chunkIndex].Data.stateBitsets[y * Chunk.Size + x] = states;

            return true;
        }

        public bool SetBlockHealth(int x, int y, float health)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return false;

            Chunks[chunkIndex].Data.healths[y * Chunk.Size + x] = health;

            return true;
        }

        public bool SetBlockLifetime(int x, int y, float lifetime)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return false;

            Chunks[chunkIndex].Data.lifetimes[y * Chunk.Size + x] = lifetime;

            return true;
        }

        public int GetBlock(int x, int y)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);

            if (Chunks[chunkIndex] == null)
                return BlockConstants.Border;

            var blockIndex = y * Chunk.Size + x;

            return Chunks[chunkIndex].Data.types[blockIndex];
        }

        // Put block with shifted base color
        public void PutBlock(int x, int y, int type)
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
                color.a);
            Chunks[chunkIndex].Dirty = true;
        }

        // Put block with shifted base color and states
        public void PutBlock(int x, int y, int type, int states)
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
                color.a, states);
            Chunks[chunkIndex].Dirty = true;
        }

        // Put block and change its health and states
        public void PutBlock(int x, int y, int type, int states, float health)
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
                color.a, states, health, 0);
            Chunks[chunkIndex].Dirty = true;
        }

        public void PutBlock(int x, int y, int type, Color32 color, int states)
        {
            UpdateOutsideChunk(ref x, ref y, out var chunkIndex);
            if (Chunks[chunkIndex] == null)
                return;
            Chunks[chunkIndex].PutBlock(x, y, type, color.r, color.g, color.b, color.a, states);
            Chunks[chunkIndex].Dirty = true;
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
            blockMoveInfo.Chunk = newChunkIndex;
            blockMoveInfo.X = ux;
            blockMoveInfo.Y = uy;

            Chunks[newChunkIndex].SetUpdatedFlag(ux, uy);
            if (destBlock != BlockConstants.Air)
                Chunks[0].SetUpdatedFlag(x, y);

            // put the old destination block at the source position (swap)
            Chunks[0].PutBlock(x, y, destBlock,
                destColorBuffer[0],
                destColorBuffer[1],
                destColorBuffer[2],
                destColorBuffer[3],
                destState,
                destHealth,
                destLifetime);

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