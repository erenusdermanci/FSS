using System;
using System.Collections.Generic;
using Blocks;
using Chunks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;
using Color = Utils.Color;

namespace DebugTools.DrawingTool
{
    public class DrawingTool : MonoBehaviour
    {
        public bool disabled;
        public ChunkManager chunkManager;

        [HideInInspector]
        public int selectedDrawBlock;

        public bool drawBrushSelection;

        [FormerlySerializedAs("selectedBrush")]
        public ToolType selectedTool;

        public BrushType selectedBrush;

        [HideInInspector]
        public int selectedState;

        public bool colorizeOnly;
        public bool overrideDefaultColors;
        public Color32 pixelColorOverride;

        [HideInInspector]
        public int boxSize;

        [HideInInspector]
        public int circleRadius;

        public Text uiCoordText;

        private Vector2i? _lastPointDrawn;
        private Vector2i? _lastPointDrawnForLine;

        private readonly UniqueQueue<Vector2i> _blockQueue = new UniqueQueue<Vector2i>();

        private readonly HashSet<Vector2i> _chunksToReload = new HashSet<Vector2i>();

        private ChunkMap<ChunkServer> _serverChunkMap;
        private ChunkMap<ChunkClient> _clientChunkMap;

        // Start is called before the first frame update
        private void Awake()
        {
            _serverChunkMap = chunkManager.ServerChunkMap;
            _clientChunkMap = chunkManager.ClientChunkMap;
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

            switch (selectedTool)
            {
                case ToolType.Point:
                    UpdateDraw(blockPosition);
                    break;
                case ToolType.Fill:
                    UpdateFill(blockPosition);
                    break;
            }

            foreach (var chunkPos in _chunksToReload)
            {
                var serverChunk = _serverChunkMap[chunkPos];
                if (serverChunk == null)
                    continue;
                var chunkDirtyRects = serverChunk.DirtyRects;
                for (var i = 0; i < chunkDirtyRects.Length; ++i)
                {
                    chunkDirtyRects[i].Reset();
                    chunkDirtyRects[i].Initialized = false;
                }
                serverChunk.Dirty = true;
                _clientChunkMap[chunkPos]?.UpdateTexture();
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
            var blockInfo = new ChunkServer.BlockInfo();
            chunk.GetBlockInfo(blockIndexInChunk, ref blockInfo);

            // Draw selected
            DrawSelectedBlock(chunk.Position, blockXInChunk, blockYInChunk);

            var r = chunk.Data.colors[blockIndexInChunk * 4];
            var g = chunk.Data.colors[blockIndexInChunk * 4 + 1];
            var b = chunk.Data.colors[blockIndexInChunk * 4 + 2];
            var a = chunk.Data.colors[blockIndexInChunk * 4 + 3];

            // Update info text
            uiCoordText.text =
                $"X: {blockXInChunk}, Y: {blockYInChunk}\n"
                + $"Type: {BlockConstants.BlockNames[blockInfo.Type]}\n"
                + $"StateBitset: {blockInfo.StateBitset}\n"
                + $"Health: {blockInfo.Health}\n"
                + $"Lifetime: {blockInfo.Lifetime}\n"
                + $"UpdatedFlag: {chunk.BlockUpdatedFlags[blockIndexInChunk]}\n"
                + $"Color: [{r},{g},{b},{a}]\n"
                + $"Chunk X:{chunk.Position.x}, Chunk Y: {chunk.Position.y}\n"
                + $"Current UpdatedFlag: {ChunkManager.UpdatedFlag}";
        }

        private void UpdateDraw(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_lastPointDrawnForLine != null && Input.GetKey(KeyCode.LeftShift))
                    DrawLine(_lastPointDrawnForLine.Value, blockPosition, false);
            }
            else if (Input.GetMouseButton(0))
            {
                if (_lastPointDrawn == null)
                    DrawBrush(blockPosition.x, blockPosition.y);
                else
                    DrawLine(_lastPointDrawn.Value, blockPosition, false);
                _lastPointDrawn = new Vector2i(blockPosition.x, blockPosition.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _lastPointDrawnForLine = _lastPointDrawn;
                _lastPointDrawn = null;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                if (_lastPointDrawnForLine != null)
                {
                    DrawDebugLine(_lastPointDrawnForLine.Value, blockPosition, UnityEngine.Color.white);
                }
            }

            Flush();
        }

        private void Flush()
        {
            while (_blockQueue.Count != 0)
            {
                var p = _blockQueue.Dequeue();
                PutBlock(p.x, p.y);
            }
        }

        private static void DrawDebugLine(Vector2i start, Vector2i end, Color32 color)
        {
            const float blockSize = 1.0f / Chunk.Size / 2;
            var xStart = start.x / (float)Chunk.Size - 0.5f + blockSize;
            var yStart = start.y / (float)Chunk.Size - 0.5f + blockSize;
            var xEnd = end.x / (float)Chunk.Size - 0.5f + blockSize;
            var yEnd = end.y / (float)Chunk.Size - 0.5f + blockSize;
            Debug.DrawLine(new Vector2(xStart, yStart), new Vector2(xEnd, yEnd), color);
        }

        private void UpdateFill(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var positionQueue = new Queue<Vector2i>();
                var processing = new HashSet<Vector2i>();
                var first = new Vector2i(blockPosition.x, blockPosition.y);
                positionQueue.Enqueue(first);
                var blockUnderCursor = GetBlockType(blockPosition.x, blockPosition.y);
                while (positionQueue.Count != 0)
                {
                    var pos = positionQueue.Dequeue();

                    var x = pos.x;
                    var y = pos.y;
                    PutBlock(x, y);

                    var right = new Vector2i(x + 1, y);
                    var left = new Vector2i(x - 1, y);
                    var up = new Vector2i(x, y + 1);
                    var down = new Vector2i(x, y - 1);
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

        private static Vector2i GetWorldPositionFromMousePosition()
        {
            var mousePos = Input.mousePosition;
            if (Camera.main is null)
            {
                throw new InvalidOperationException();
            }
            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            return new Vector2i((int) Mathf.Floor((worldPos.x + 0.5f) * 64.0f), (int) Mathf.Floor((worldPos.y + 0.5f) * 64.0f));
        }

        private void DrawBox(int x, int y, bool immediate = true)
        {
            var px = x - boxSize / 2;
            var py = y - boxSize / 2;

            for (var i = px; i < px + boxSize; i++)
            {
                for (var j = py; j < py + boxSize; j++)
                {
                    PutBlock(i, j, immediate);
                }
            }
        }

        private void DrawCircle(int x, int y, bool immediate = true)
        {
            for (var i = -circleRadius; i <= circleRadius; ++i)
            {
                for (var j = -circleRadius; j <= circleRadius; ++j)
                {
                    if (j * j + i * i <= circleRadius * circleRadius)
                        PutBlock(x + i, j + y, immediate);
                }
            }
        }

        private void DrawLine(Vector2i flooredPosVec2Start, Vector2i flooredPosVec2End, bool immediate = true)
        {
            var x = flooredPosVec2Start.x;
            var y = flooredPosVec2Start.y;
            var x2 = flooredPosVec2End.x;
            var y2 = flooredPosVec2End.y;
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
                DrawBrush(x, y, immediate);
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

        private void DrawBlockGrid(Vector2i chunkPos)
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

        private void DrawSelectedBlock(Vector2i chunkPos, int x, int y)
        {
            var selectColor = UnityEngine.Color.red;
            var xOffset = (x - boxSize / 2) / (float)Chunk.Size;
            var yOffset = (y - boxSize / 2) / (float)Chunk.Size;
            var blockSize = (1 / (float)Chunk.Size) * boxSize;

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

        private void DrawBrush(int x, int y, bool immediate = true)
        {
            switch (selectedBrush)
            {
                case BrushType.Box:
                    DrawBox(x, y, immediate);
                    break;
                case BrushType.Circle:
                    DrawCircle(x, y, immediate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PutBlock(int worldX, int worldY, bool immediate = true)
        {
            if (!immediate)
            {
                _blockQueue.Enqueue(new Vector2i(worldX, worldY));
                return;
            }

            var blockColor = GetBlockColor();

            var r = blockColor.r;
            var g = blockColor.g;
            var b = blockColor.b;
            if (selectedState == 1)
            {
                blockColor = BlockConstants.FireColor;
                var shiftAmount = Helpers.GetRandomShiftAmount(BlockConstants.FireColorMaxShift);
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
                if (BlockConstants.BlockDescriptors[selectedDrawBlock].InitialStates != 0 && selectedState == 0)
                    selectedState = BlockConstants.BlockDescriptors[selectedDrawBlock].InitialStates;
                chunk.PutBlock(blockXInChunk, blockYInChunk, selectedDrawBlock, r, g, b, blockColor.a,
                    selectedState, BlockConstants.BlockDescriptors[selectedDrawBlock].BaseHealth, 0);
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

        private ChunkServer GetChunkFromWorld(float worldX, float worldY)
        {
            var chunkPosition = new Vector2i((int) Mathf.Floor(worldX / 64.0f), (int) Mathf.Floor(worldY / 64.0f));
            return !_serverChunkMap.Contains(chunkPosition) ? null : _serverChunkMap[chunkPosition];
        }

        private Color GetBlockColor()
        {
            if (overrideDefaultColors)
                return new Color(pixelColorOverride.r, pixelColorOverride.g, pixelColorOverride.b, pixelColorOverride.a);

            var block = selectedDrawBlock;
            var shiftAmount = Helpers.GetRandomShiftAmount(BlockConstants.BlockDescriptors[block].ColorMaxShift);
            var color = BlockConstants.BlockDescriptors[block].Color;
            return new Color(
                Helpers.ShiftColorComponent(color.r, shiftAmount),
                Helpers.ShiftColorComponent(color.g, shiftAmount),
                Helpers.ShiftColorComponent(color.b, shiftAmount),
                color.a);
        }
    }
}
