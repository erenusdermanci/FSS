using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using Chunks;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Random = System.Random;

namespace DebugTools
{
    public class DrawingTool : MonoBehaviour
    {
        [Serializable]
        public enum DrawType
        {
            Point,
            Box,
            Line,
            Fill
        }

        public bool disabled;
        public ChunkManager chunkManager;

        [HideInInspector]
        public int selectedDrawBlock;
        public bool drawBrushSelection;
        public DrawType selectedBrush;
        [HideInInspector]
        public int selectedState;

        public bool colorizeOnly;
        public bool overrideDefaultColors;
        public Color32 pixelColorOverride;

        [HideInInspector]
        public int boxSize;

        public Text uiCoordText;

        private bool _userDrawingLine;
        private Vector2? _drawStartPos;
        private Vector2? _drawEndPos;

        private Random _rng;

        private readonly HashSet<Vector2> _chunksToReload = new HashSet<Vector2>();

        private ChunkMap _chunkMap;

        // Start is called before the first frame update
        private void Awake()
        {
            _userDrawingLine = false;
            _rng = new Random();
            _chunkMap = chunkManager.ChunkMap;
        }

        // Update is called once per frame
        private void Update()
        {
            if (disabled)
                return;

            var blockPosition = GetWorldPositionFromMousePosition();

            if (drawBrushSelection)
            {
                DrawSelected(blockPosition.x, blockPosition.y);
            }

            if (GlobalDebugConfig.StaticGlobalConfig.outlineChunks)
            {
                DrawChunkGrid(blockPosition.x, blockPosition.y);
            }

            switch (selectedBrush)
            {
                case DrawType.Point:
                    UpdateDrawPixel(blockPosition);
                    break;
                case DrawType.Box:
                    UpdateDrawBox(blockPosition);
                    break;
                case DrawType.Line:
                    UpdateDrawLine(blockPosition);
                    break;
                case DrawType.Fill:
                    UpdateFill(blockPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var chunkPos in _chunksToReload)
            {
                var chunk = _chunkMap[chunkPos];
                if (chunk == null)
                    continue;
                chunk.UpdateTexture();
                var chunkDirtyRects = chunk.DirtyRects;
                foreach (var chunkDirtyRect in chunkDirtyRects)
                    chunkDirtyRect.Reset();
                chunk.Dirty = true;
            }
            _chunksToReload.Clear();
        }

        private void DrawChunkGrid(float worldX, float worldY)
        {
            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            // Draw block grid in chunk
            DrawBlockGrid(chunk.Position);
        }

        private void DrawSelected(float worldX, float worldY)
        {
            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod((int) worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod((int) worldY, Chunk.Size);
            var blockIndexInChunk = blockYInChunk * Chunk.Size + blockXInChunk;
            var blockInfo = new Chunk.BlockInfo();
            chunk.GetBlockInfo(blockIndexInChunk, ref blockInfo);

            // Draw selected
            DrawSelectedBlock(chunk.Position, blockXInChunk, blockYInChunk);

            // Update info text
            uiCoordText.text =
                $"X: {blockXInChunk}, Y: {blockYInChunk}\n"
                + $"Type: {BlockConstants.BlockNames[blockInfo.Type]}\n"
                + $"StateBitset: {blockInfo.StateBitset}\n"
                + $"Health: {blockInfo.Health}\n"
                + $"Lifetime: {blockInfo.Lifetime}\n"
                + $"Chunk X:{chunk.Position.x}, Chunk Y: {chunk.Position.y}";
        }

        private void UpdateDrawPixel(Vector2 blockPosition)
        {
            if (Input.GetMouseButton(0))
            {
                PutBlock((int) blockPosition.x, (int) blockPosition.y, selectedDrawBlock, GetBlockColor(), selectedState);
            }
        }

        private void UpdateDrawBox(Vector2 blockPosition)
        {
            if (Input.GetMouseButton(0))
            {
                DrawBox(blockPosition);
            }
        }

        private void UpdateDrawLine(Vector2 blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!_userDrawingLine)
                {
                    _userDrawingLine = true;
                    _drawStartPos = blockPosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_userDrawingLine)
                {
                    _drawEndPos = blockPosition;

                    if (_drawStartPos?.x != null)
                    {
                        DrawLine(_drawStartPos.Value, _drawEndPos.Value);
                    }

                    _userDrawingLine = false;
                    _drawStartPos = null;
                    _drawEndPos = null;
                }
            }

            if (_userDrawingLine)
            {
                if (_drawStartPos != null)
                {
                    var xStart = _drawStartPos.Value.x / Chunk.Size - 0.5f;
                    var yStart = _drawStartPos.Value.y / Chunk.Size - 0.5f;
                    var xEnd = blockPosition.x / Chunk.Size - 0.5f;
                    var yEnd = blockPosition.y / Chunk.Size - 0.5f;
                    Debug.DrawLine(new Vector2(xStart, yStart), new Vector2(xEnd, yEnd), Color.white);
                }
            }
        }

        private void UpdateFill(Vector2 blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var positionQueue = new Queue<Vector2Int>();
                var processing = new HashSet<Vector2Int>();
                var first = new Vector2Int((int) blockPosition.x, (int) blockPosition.y);
                positionQueue.Enqueue(first);
                var blockUnderCursor = GetBlockType((int) blockPosition.x, (int) blockPosition.y);
                while (positionQueue.Count != 0)
                {
                    var pos = positionQueue.Dequeue();

                    var x = pos.x;
                    var y = pos.y;
                    PutBlock(x, y, selectedDrawBlock, GetBlockColor(), selectedState);

                    var right = new Vector2Int(x + 1, y);
                    var left = new Vector2Int(x - 1, y);
                    var up = new Vector2Int(x, y + 1);
                    var down = new Vector2Int(x, y - 1);
                    if (GetBlockType(right.x, right.y) == blockUnderCursor && !processing.Contains(right))
                    {
                        positionQueue.Enqueue(right);
                        processing.Add(right);
                    }
                    if (GetBlockType(left.x, left.y) == blockUnderCursor && !processing.Contains(left))
                    {
                        positionQueue.Enqueue(left);
                        processing.Add(left);
                    }
                    if (GetBlockType(up.x, up.y) == blockUnderCursor && !processing.Contains(up))
                    {
                        positionQueue.Enqueue(up);
                        processing.Add(up);
                    }
                    if (GetBlockType(down.x, down.y) == blockUnderCursor && !processing.Contains(down))
                    {
                        positionQueue.Enqueue(down);
                        processing.Add(down);
                    }
                }
            }
        }

        private static Vector2 GetWorldPositionFromMousePosition()
        {
            var mousePos = Input.mousePosition;
            if (Camera.main is null)
            {
                throw new InvalidOperationException();
            }
            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            return new Vector2((int) Mathf.Floor((worldPos.x + 0.5f) * 64.0f), (int) Mathf.Floor((worldPos.y + 0.5f) * 64.0f));
        }

        private void DrawBox(Vector2 blockPosition)
        {
            var worldX = (int) blockPosition.x;
            var worldY = (int) blockPosition.y;
            var px = worldX - boxSize / 2;
            var py = worldY - boxSize / 2;

            for (var i = px; i < px + boxSize; i++)
            {
                for (var j = py; j < py + boxSize; j++)
                {
                    PutBlock(i, j, selectedDrawBlock, GetBlockColor(), selectedState);
                }
            }
        }

        private void DrawLine(Vector2 flooredPosVec2Start, Vector2 flooredPosVec2End)
        {
            Bresenham((int) flooredPosVec2Start.x, (int) flooredPosVec2Start.y, (int) flooredPosVec2End.x, (int) flooredPosVec2End.y);
        }

        // Implementation of Bresenham's line algorithm
        private void Bresenham(int x, int y, int x2, int y2)
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
                PutBlock(x, y, selectedDrawBlock, GetBlockColor(), selectedState);
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

            if (selectedBrush.Equals(DrawType.Box))
            {
                xOffset = (x - boxSize / 2) / (float)Chunk.Size;
                yOffset = (y - boxSize / 2) / (float)Chunk.Size;
                blockSize *= boxSize;
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

        private void PutBlock(int worldX, int worldY, int selectedBlock, Color32 blockColor, int states)
        {
            var r = blockColor.r;
            var g = blockColor.g;
            var b = blockColor.b;
            if (selectedState == 1)
            {
                blockColor = BlockConstants.FireColor;
                var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockConstants.FireColorMaxShift);
                r = Helpers.ShiftColorComponent(blockColor.r, shiftAmount);
                g = Helpers.ShiftColorComponent(blockColor.g, shiftAmount);
                b = Helpers.ShiftColorComponent(blockColor.b, shiftAmount);
            }

            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod(worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(worldY, Chunk.Size);

            if (colorizeOnly)
            {
                var i = blockYInChunk * Chunk.Size + blockXInChunk;
                chunk.Data.colors[i * 4] = pixelColorOverride.r;
                chunk.Data.colors[i * 4 + 1] = pixelColorOverride.g;
                chunk.Data.colors[i * 4 + 2] = pixelColorOverride.b;
                chunk.Data.colors[i * 4 + 3] = pixelColorOverride.a;
            }
            else
            {
                if (BlockConstants.BlockDescriptors[selectedBlock].InitialStates != 0 && selectedState == 0)
                    selectedState = BlockConstants.BlockDescriptors[selectedBlock].InitialStates;
                chunk.PutBlock(blockXInChunk, blockYInChunk, selectedBlock, r, g, b, blockColor.a,
                    states, BlockConstants.BlockDescriptors[selectedBlock].BaseHealth, 0);
            }

            _chunksToReload.Add(chunk.Position);
        }

        private int GetBlockType(int worldX, int worldY)
        {
            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return -1;

            var blockXInChunk = Helpers.Mod(worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(worldY, Chunk.Size);

            return chunk.GetBlockType(blockYInChunk * Chunk.Size + blockXInChunk);
        }

        private Chunk GetChunkFromWorld(float worldX, float worldY)
        {
            var chunkPosition = new Vector2((int) Mathf.Floor(worldX / 64.0f), (int) Mathf.Floor(worldY / 64.0f));
            return !_chunkMap.Contains(chunkPosition) ? null : _chunkMap[chunkPosition];
        }

        private Color32 GetBlockColor()
        {
            if (overrideDefaultColors)
                return pixelColorOverride;

            var block = selectedDrawBlock;
            var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockConstants.BlockDescriptors[block].ColorMaxShift);
            var color = BlockConstants.BlockDescriptors[block].Color;
            return new Color32(
                Helpers.ShiftColorComponent(color.r, shiftAmount),
                Helpers.ShiftColorComponent(color.g, shiftAmount),
                Helpers.ShiftColorComponent(color.b, shiftAmount),
                color.a);
        }
    }
}
