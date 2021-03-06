using System;
using System.Collections.Generic;
using DataComponents;
using MonoBehaviours;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using static BlockConstants;

public class DrawingTool : MonoBehaviour
{
    [Serializable]
    public enum DrawType
    {
        Pixel,
        Box,
        Line,
        ColorPixel
    }

    public bool Enabled;
    public ChunkManager ChunkManager;

    public Blocks SelectedDrawBlock;
    public DrawType SelectedBrush;
    public bool OverrideDefaultColors;
    public Color32 PixelColorOverride;

    [Range(0, 64)]
    public int BoxSize;

    public Text UICoordText;

    private bool userDrawingLine;
    private Vector2? drawStartPos;
    private Vector2? drawEndPos;

    private System.Random _rng;

    private HashSet<Vector2> _chunksToReload = new HashSet<Vector2>();

    // Start is called before the first frame update
    void Awake()
    {
        userDrawingLine = false;
        _rng = new System.Random();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Enabled)
            return;

        var mousePos = GetAdjustedWorldMousePosition();
        var flooredMousePos = new Vector2(Mathf.Floor(mousePos.x), Mathf.Floor(mousePos.y));
        if (!ChunkManager.ChunkGrid.ChunkMap.ContainsKey(flooredMousePos)) // Nothing to draw
            return;

        var neighborhood = GetNeighborhood(flooredMousePos);
        var xOffset = (int)((mousePos.x - flooredMousePos.x) * Chunk.Size);
        var yOffset = (int)((mousePos.y - flooredMousePos.y) * Chunk.Size);

        if (GlobalDebugConfig.StaticGlobalConfig.OutlineChunks)
        {
            // Draw block grid in chunk
            DrawBlockGrid(flooredMousePos);
            // Draw selected
            DrawSelectedBlock(flooredMousePos, xOffset, yOffset);

            UICoordText.text = $@"x: {xOffset}, y: {yOffset}
                                 chunk x:{flooredMousePos.x}, chunk y: {flooredMousePos.y}";
        }

        switch (SelectedBrush)
        {
            case DrawType.Pixel:
                UpdateDrawPixel(neighborhood, xOffset, yOffset);
                break;
            case DrawType.Box:
                UpdateDrawBox(neighborhood, xOffset, yOffset);
                break;
            case DrawType.Line:
                UpdateDrawLine(neighborhood, mousePos);
                break; 
            case DrawType.ColorPixel:
                UpdateColorPixel(neighborhood, xOffset, yOffset);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        foreach (var chunkPosition in _chunksToReload)
        {
            ChunkManager.ChunkGrid.ChunkMap[chunkPosition].UpdateTexture();
        }
        _chunksToReload.Clear();
    }

    private void UpdateDrawPixel(ChunkNeighborhood neighborhood, int xOffset, int yOffset)
    {
        if (Input.GetMouseButton(0))
        {
            DrawPixel(neighborhood, xOffset, yOffset);
        }
    }

    private void UpdateDrawBox(ChunkNeighborhood neighborhood, int xOffset, int yOffset)
    {
        if (Input.GetMouseButton(0))
        {
            DrawBox(neighborhood, xOffset, yOffset);
        }
    }

    private void UpdateDrawLine(ChunkNeighborhood neighborhood, Vector2 mousePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!userDrawingLine)
            {
                userDrawingLine = true;
                drawStartPos = mousePos;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (userDrawingLine)
            {
                drawEndPos = mousePos;

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
    }

    private void UpdateColorPixel(ChunkNeighborhood neighborhood, int xOffset, int yOffset)
    {
        if (Input.GetMouseButton(0))
        {
            ColorPixel(neighborhood, xOffset, yOffset);
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
        var centralChunk = ChunkManager.ChunkGrid.ChunkMap[flooredPosVec2];
        return new ChunkNeighborhood(ChunkManager.ChunkGrid, centralChunk);
    }

    private void DrawPixel(ChunkNeighborhood neighborhood, int x, int y)
    {
        PutBlock(neighborhood, x, y, (int)SelectedDrawBlock, GetBlockColor());
    }

    private void DrawBox(ChunkNeighborhood neighborhood, int x, int y)
    {
        var px = x - BoxSize / 2;
        var py = y - BoxSize / 2;

        for (var i = px; i < px + BoxSize; i++)
        {
            for (var j = py; j < py + BoxSize; j++)
            {
                PutBlock(neighborhood, i, j, (int)SelectedDrawBlock, GetBlockColor());
            }
        }
    }

    private void DrawLine(ChunkNeighborhood neighborhood, Vector2 flooredPosVec2Start, Vector2 flooredPosVec2End)
    {
        var xOffsetInChunkStart = (int)(((float)drawStartPos?.x - flooredPosVec2Start.x) * Chunk.Size);
        var yOffsetInChunkStart = (int)(((float)drawStartPos?.y - flooredPosVec2Start.y) * Chunk.Size);

        var xOffsetInChunkEnd = (int)(((float)drawEndPos?.x - flooredPosVec2End.x) * Chunk.Size);
        var yOffsetInChunkEnd = (int)(((float)drawEndPos?.y - flooredPosVec2End.y) * Chunk.Size);

        Bresenham(neighborhood, xOffsetInChunkStart, yOffsetInChunkStart, xOffsetInChunkEnd, yOffsetInChunkEnd);
    }

    // Implementation of Bresenham's line algorithm
    private void Bresenham(ChunkNeighborhood neighborhood, int x, int y, int x2, int y2)
    {
        var w = x2 - x;
        var h = y2 - y;
        var dx1 = 0;
        var dy1 = 0;
        var dx2 = 0;
        var dy2 = 0;
        if (w < 0) dx1 = -1;
        else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1;
        else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1;
        else if (w > 0) dx2 = 1;
        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1;
            else if (h > 0) dy2 = 1;
            dx2 = 0;
        }

        var numerator = longest >> 1;
        for (var i = 0; i <= longest; i++)
        {
            PutBlock(neighborhood, x, y, (int) SelectedDrawBlock, GetBlockColor());
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
        for (var x = 0; x < Chunk.Size + 1; x++)
        {
            var xOffset = x / (float)Chunk.Size - 0.5f;
            Debug.DrawLine(new Vector3(chunkPos.x + xOffset, chunkPos.y - 0.5f), new Vector3(chunkPos.x + xOffset, chunkPos.y + 0.5f), gridColor);
        }

        for (var y = 0; y < Chunk.Size + 1; y++)
        {
            var yOffset = y / (float)Chunk.Size - 0.5f;
            Debug.DrawLine(new Vector3(chunkPos.x - 0.5f, chunkPos.y + yOffset), new Vector3(chunkPos.x + 0.5f, chunkPos.y + yOffset), gridColor);
        }
    }

    private void DrawSelectedBlock(Vector2 chunkPos, int x, int y)
    {
        var selectColor = Color.red;
        var xOffset = x / (float)Chunk.Size;
        var yOffset = y / (float)Chunk.Size;
        var blockSize = 1 / (float)Chunk.Size;

        if (SelectedBrush.Equals(DrawType.Box))
        {
            xOffset = (x - BoxSize / 2) / (float)Chunk.Size;
            yOffset = (y - BoxSize / 2) / (float)Chunk.Size;
            blockSize *= BoxSize;
        }

        // along x axis bottom
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset),
                        new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset), selectColor);
        // along y axis right side
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset),
                        new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
        // along y left side
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset),
                        new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
        // along x axis top
        Debug.DrawLine(new Vector3(chunkPos.x - 0.5f + xOffset, chunkPos.y - 0.5f + yOffset + blockSize),
                        new Vector3(chunkPos.x - 0.5f + xOffset + blockSize, chunkPos.y - 0.5f + yOffset + blockSize), selectColor);
    }

    private void PutBlock(ChunkNeighborhood neighborhood, int x, int y, int selectedDrawBlock, Color32 blockColor)
    {
        Chunk chunkWritten = null;
        neighborhood.PutBlock(x, y, selectedDrawBlock, blockColor, ref chunkWritten);

        if (chunkWritten != null)
            _chunksToReload.Add(chunkWritten.Position);
    }

    private Color32 GetBlockColor()
    {
        if (OverrideDefaultColors)
            return PixelColorOverride;

        var block = (int) SelectedDrawBlock;
        var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockColorMaxShift[block]);
        var color = BlockColors[block];
        return new Color32(
            Helpers.ShiftColorComponent(color.r, shiftAmount),
            Helpers.ShiftColorComponent(color.g, shiftAmount),
            Helpers.ShiftColorComponent(color.b, shiftAmount),
            color.a);
    }

    private void ColorPixel(ChunkNeighborhood neighborhood, int x, int y)
    {
        var i = y * Chunk.Size + x;
        neighborhood.Chunks[0].blockData.colors[i * 4] = PixelColorOverride.r;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 1] = PixelColorOverride.g;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 2] = PixelColorOverride.b;
        neighborhood.Chunks[0].blockData.colors[i * 4 + 3] = PixelColorOverride.a;
        _chunksToReload.Add(neighborhood.Chunks[0].Position);
    }
}
