using DataComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class ChunkNeighborhood
{
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
            GetNeighborChunkBlocksColors(grid, centralChunk, -1, -1),
            GetNeighborChunkBlocksColors(grid, centralChunk, 0, -1),
            GetNeighborChunkBlocksColors(grid, centralChunk, 1, -1),
            GetNeighborChunkBlocksColors(grid, centralChunk, -1, 0),
            GetNeighborChunkBlocksColors(grid, centralChunk, 1, 0),
            GetNeighborChunkBlocksColors(grid, centralChunk, -1, 1),
            GetNeighborChunkBlocksColors(grid, centralChunk, 0, 1),
            GetNeighborChunkBlocksColors(grid, centralChunk, 1, 1)
        };
    }

    public int GetBlock(int x, int y, bool current = false)
    {
        var chunkIndex = 0;
        UpdateOutsideChunk(ref x, ref y, ref chunkIndex);

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

    public void PutBlock(int x, int y, int type, bool checkOutsideChunk = false, bool setCooldown = false)
    {
        var chunkIndex = 0;
        if (checkOutsideChunk)
            UpdateOutsideChunk(ref x, ref y, ref chunkIndex);

        if (Chunks[chunkIndex] == null)
            return;

        var i = y * Chunk.Size + x;
        Chunks[chunkIndex].BlockColors[i * 4] = BlockColors[type].r;
        Chunks[chunkIndex].BlockColors[i * 4 + 1] = BlockColors[type].g;
        Chunks[chunkIndex].BlockColors[i * 4 + 2] = BlockColors[type].b;
        Chunks[chunkIndex].BlockColors[i * 4 + 3] = BlockColors[type].a;
        Chunks[chunkIndex].BlockTypes[i] = type;

        if (setCooldown || chunkIndex != 0)
            Chunks[chunkIndex].BlockUpdateCooldowns[i] = 1;
    }

    public static void UpdateOutsideChunk(ref int x, ref int y, ref int chunkIndex)
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

    public static Chunk GetNeighborChunkBlocksColors(ChunkGrid grid, Chunk origin, int xOffset, int yOffset)
    {
        var neighborPosition = new Vector2(origin.Position.x + xOffset, origin.Position.y + yOffset);
        if (grid.ChunkMap.ContainsKey(neighborPosition))
        {
            return grid.ChunkMap[neighborPosition];
        }
        return null;
    }
}
