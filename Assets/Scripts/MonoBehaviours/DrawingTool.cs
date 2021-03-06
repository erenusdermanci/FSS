using System;
using DataComponents;
using MonoBehaviours;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using static Constants;

public class DrawingTool : MonoBehaviour
{
    public bool Enabled;
    public ChunkManager ChunkManager;

    public Blocks SelectedDrawBlock;
    public DrawType SelectedBrush;
    public Color32 ColorPixelColor;

    [Range(0, 64)]
    public int BoxSize;

    public Text UICoordText;

    private bool userDrawingLine;
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
            var mousePos = GetAdjustedWorldMousePosition();
            var flooredMousePos = new Vector2(Mathf.Floor(mousePos.x), Mathf.Floor(mousePos.y));
            if (!ChunkManager._chunkGrid.ChunkMap.ContainsKey(flooredMousePos)) // Nothing to draw
                return;

            var neighborhood = GetNeighborhood(flooredMousePos);
            var xOffset = (int)((mousePos.x - flooredMousePos.x) * Chunk.Size);
            var yOffset = (int)((mousePos.y - flooredMousePos.y) * Chunk.Size);

            // Draw block grid in chunk
            DrawBlockGrid(flooredMousePos);
            // Draw selected
            DrawSelectedBlock(flooredMousePos, xOffset, yOffset);

            UICoordText.text = $@"x: {xOffset}, y: {yOffset}
                                 chunk x:{flooredMousePos.x}, chunk y: {flooredMousePos.y}";

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

                switch (SelectedBrush)
                {
                    case DrawType.Pixel:
                        DrawPixel(neighborhood, xOffset, yOffset);
                        break;
                    case DrawType.Box:
                        DrawBox(neighborhood, xOffset, yOffset);
                        break;
                    case DrawType.ColorPixel:
                        ColorPixel(neighborhood, xOffset, yOffset);
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
        var blockColor = BlockColors[(int)SelectedDrawBlock];
        neighborhood.PutBlock(x, y, (int)SelectedDrawBlock, blockColor);
    }

    private void DrawBox(ChunkNeighborhood neighborhood, int x, int y)
    {
        var blockColor = BlockColors[(int)SelectedDrawBlock];
        var px = x - BoxSize / 2;
        var py = y - BoxSize / 2;

        for (int i = px; i < px + BoxSize; i++)
        {
            for (int j = py; j < py + BoxSize; j++)
            {
                neighborhood.PutBlock(i, j, (int)SelectedDrawBlock, blockColor);
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
        var blockColor = BlockColors[(int)SelectedDrawBlock];
        var w = x2 - x;
        var h = y2 - y;
        var dx1 = 0;
        var dy1 = 0;
        var dx2 = 0;
        var dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (var i = 0; i <= longest; i++)
        {
            neighborhood.PutBlock(x, y, (int)SelectedDrawBlock, blockColor);
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

    private void DrawBlockGrid(Vector2 chunkPos)
    {
        // hardcoded but its debug so its ok
        var gridColor = new Color32(255, 255, 255, 90);
        for (var x = 0; x < Chunk.Size; x++)
        {
            var xOffset = (float)x / (float)Chunk.Size - 0.5f;
            Debug.DrawLine(new Vector3(chunkPos.x + xOffset, chunkPos.y - 0.5f), new Vector3(chunkPos.x + xOffset, chunkPos.y + 0.5f), gridColor);
        }

        for (var y = 0; y < Chunk.Size; y++)
        {
            var yOffset = (float)y / (float)Chunk.Size - 0.5f;
            Debug.DrawLine(new Vector3(chunkPos.x - 0.5f, chunkPos.y + yOffset), new Vector3(chunkPos.x + 0.5f, chunkPos.y + yOffset), gridColor);
        }
    }

    private void DrawSelectedBlock(Vector2 chunkPos, int x, int y)
    {
        var selectColor = Color.red;
        var xOffset = (float)x / (float)Chunk.Size;
        var yOffset = (float)y / (float)Chunk.Size;
        var blockSize = (float)1 / (float)Chunk.Size;

        if (SelectedBrush.Equals(DrawType.Box))
        {
            xOffset = (float)(x - (int)(BoxSize / 2)) / (float)(Chunk.Size);
            yOffset = (float)(y - (int)(BoxSize / 2)) / (float)(Chunk.Size);
            blockSize *= BoxSize;
        }

        // along x axis bottom
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset), new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset), selectColor);
        // along y axis right side
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset), new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
        // along y left side
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset), new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
        // along x axis top
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset + blockSize), new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
    }

    private void ColorPixel(ChunkNeighborhood neighborhood, int x, int y)
    {
        var i = y * Chunk.Size + x;
        neighborhood.Chunks[0].blockData.colors[i * 4] = ColorPixelColor.r;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 1] = ColorPixelColor.g;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 2] = ColorPixelColor.b;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 3] = ColorPixelColor.a;
    }

    [Serializable]
    public enum DrawType
    {
        Pixel,
        Box,
        Line,
        ColorPixel
    }
}
