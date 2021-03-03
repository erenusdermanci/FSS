using DataComponents;
using MonoBehaviours;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class DrawingTool : MonoBehaviour
{
    public bool Enabled;
    public ChunkManager ChunkManager;

    public Blocks SelectedDrawBlock;
    public DrawType SelectedBrush;

    [Range(0, 64)]
    public int BoxSize;

    private bool userDrawingLine;
    private readonly int linePixelMaxLength = Chunk.Size;
    private Vector2? drawStartPos = null;
    private Vector2? drawEndPos = null;

    // Start is called before the first frame update
    void Awake()
    {
        userDrawingLine = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Enabled)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (SelectedBrush.Equals(DrawType.Line) && !userDrawingLine)
                {
                    userDrawingLine = true;
                    drawStartPos = GetAdjustedWorldMousePosition();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (SelectedBrush.Equals(DrawType.Line) && userDrawingLine)
                {
                    drawEndPos = GetAdjustedWorldMousePosition();

                    // Draw the line
                    var flooredPosVec2Start = new Vector2(Mathf.Floor((float)drawStartPos?.x), Mathf.Floor((float)drawStartPos?.y));
                    var flooredPosVec2End = new Vector2(Mathf.Floor((float)drawEndPos?.x), Mathf.Floor((float)drawEndPos?.y));
                    var neighborhood = GetNeighborhood(flooredPosVec2Start);
                    DrawLine(neighborhood, flooredPosVec2Start, flooredPosVec2End);

                    // Clear variables
                    userDrawingLine = false;
                    drawStartPos = null;
                    drawEndPos = null;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (SelectedBrush.Equals(DrawType.Line))
                {
                    return;
                }

                var adjustedWorldPos = GetAdjustedWorldMousePosition();
                var flooredPosVec2 = new Vector2(Mathf.Floor(adjustedWorldPos.x), Mathf.Floor(adjustedWorldPos.y));

                // Check if chunk exists at this position, return otherwise
                if (!ChunkManager._chunkGrid.ChunkMap.ContainsKey(flooredPosVec2))
                    return;

                // Now that we know the user clicked in a valid spot, let's set up the chunk
                // neighborhood and get the exact pixel where he clicked
                var neighborhood = GetNeighborhood(flooredPosVec2);

                var xOffsetInChunk = (int)((adjustedWorldPos.x - flooredPosVec2.x) * Chunk.Size);
                var yOffsetInChunk = (int)((adjustedWorldPos.y - flooredPosVec2.y) * Chunk.Size);

                switch (SelectedBrush)
                {
                    case DrawType.Pixel:
                        DrawPixel(neighborhood, xOffsetInChunk, yOffsetInChunk);
                        break;
                    case DrawType.Box:
                        DrawBox(neighborhood, xOffsetInChunk, yOffsetInChunk);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private Vector2 GetAdjustedWorldMousePosition()
    {
        var mousePos = Input.mousePosition;
        var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        return new Vector2(worldPos.x + 0.5f, worldPos.y + 0.5f);
    }

    private ChunkNeighborhood GetNeighborhood(Vector2 flooredPosVec2)
    {
        var centralChunk = ChunkManager._chunkGrid.ChunkMap[flooredPosVec2];
        return new ChunkNeighborhood(ChunkManager._chunkGrid, centralChunk);
    }

    private void DrawPixel(ChunkNeighborhood neighborhood, int x, int y)
    {
        neighborhood.PutBlock(x, y, (int)SelectedDrawBlock);
    }

    private void DrawBox(ChunkNeighborhood neighborhood, int x, int y)
    {
        var px = x - BoxSize / 2;
        var py = y - BoxSize / 2;

        for (int i = px; i < px + BoxSize; i++)
        {
            for (int j = py; j < py + BoxSize; j++)
            {
                neighborhood.PutBlock(i, j, (int)SelectedDrawBlock, true);
            }
        }
    }

    private void DrawLine(ChunkNeighborhood neighborhood, Vector2 flooredPosVec2Start, Vector2 flooredPosVec2End)
    {
        var xOffsetInChunkStart = (int)(((float)drawStartPos?.x - flooredPosVec2Start.x) * Chunk.Size);
        var yOffsetInChunkStart = (int)(((float)drawStartPos?.y - flooredPosVec2Start.y) * Chunk.Size);

        var xOffsetInChunkEnd = (int)(((float)drawEndPos?.x - flooredPosVec2End.x) * Chunk.Size);
        var yOffsetInChunkEnd = (int)(((float)drawEndPos?.y - flooredPosVec2End.y) * Chunk.Size);

        // Implementation of Bresenham's line algorithm
        Bresenham(neighborhood, xOffsetInChunkStart, yOffsetInChunkStart, xOffsetInChunkEnd, yOffsetInChunkEnd);
    }

    private void Bresenham(ChunkNeighborhood neighborhood, int x, int y, int x2, int y2)
    {
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Math.Abs(w);
        int shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            neighborhood.PutBlock(x, y, (int)SelectedDrawBlock, true);
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }
}

[Serializable]
public enum DrawType
{
    Pixel,
    Box,
    Line
}
