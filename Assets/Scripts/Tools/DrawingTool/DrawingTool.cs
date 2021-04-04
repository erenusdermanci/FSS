using System;
using System.Collections.Generic;
using Blocks;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using static Chunks.ChunkLayer;
using Color = Utils.Color;

namespace Tools.DrawingTool
{
    public class DrawingTool : MonoBehaviour
    {
        public ChunkLayer[] chunkLayers;

        public ClientCollisionManager clientCollisionManager;

        [HideInInspector]
        public int selectedDrawBlock;

        public bool drawBrushSelection;

        public DrawingToolType selectedDrawingTool;
        public DrawingBrushType selectedDrawingBrush;

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

        private ChunkLayerType _currentLayer = ChunkLayerType.Foreground;

        private void Update()
        {
            if (GlobalDebugConfig.StaticGlobalConfig.disableDrawingTool)
                return;

            var blockPosition = GetWorldPositionFromMousePosition();

            if (Input.GetKey(KeyCode.LeftControl))
                _currentLayer = ChunkLayerType.Background;
            else
                _currentLayer = ChunkLayerType.Foreground;

            if (drawBrushSelection)
            {
                DrawSelected(blockPosition.x, blockPosition.y);
            }

            if (GlobalDebugConfig.StaticGlobalConfig.outlineChunks)
            {
                DrawChunkGrid(blockPosition.x, blockPosition.y);
            }

            switch (selectedDrawingTool)
            {
                case DrawingToolType.Point:
                    UpdateDraw(blockPosition);
                    break;
                case DrawingToolType.Fill:
                    UpdateFill(blockPosition);
                    break;
            }

            foreach (var chunkPos in _chunksToReload)
            {
                var serverChunk = chunkLayers[(int)_currentLayer].ServerChunkMap[chunkPos];
                if (serverChunk != null)
                {
                    var chunkDirtyRects = serverChunk.DirtyRects;
                    for (var i = 0; i < chunkDirtyRects.Length; ++i)
                    {
                        chunkDirtyRects[i].Reset();
                        chunkDirtyRects[i].Initialized = false;
                    }

                    serverChunk.Dirty = true;
                }

                var clientChunk = chunkLayers[(int)_currentLayer].ClientChunkMap[chunkPos];
                if (clientChunk != null)
                {
                    clientCollisionManager.QueueChunkCollisionGeneration(clientChunk);
                    clientChunk.UpdateTexture();
                }
            }
            _chunksToReload.Clear();
        }

        private void DrawChunkGrid(float worldX, float worldY)
        {
            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            DrawBlockGrid(chunk.Position);
        }

        private static void DrawBlockGrid(Vector2i chunkPos)
        {
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

        private void DrawSelected(float worldX, float worldY)
        {
            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod((int) worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod((int) worldY, Chunk.Size);
            var blockIndexInChunk = blockYInChunk * Chunk.Size + blockXInChunk;
            var blockInfo = new Block();
            chunk.GetBlockInfo(blockIndexInChunk, ref blockInfo);

            switch (selectedDrawingBrush)
            {
                case DrawingBrushType.Box:
                {
                    var blockSize = 1f / Chunk.Size * boxSize;
                    DebugDraw.Rectangle(chunk.Position.x - 0.5f + (blockXInChunk - boxSize / 2f) / Chunk.Size,
                        chunk.Position.y - 0.5f + (blockYInChunk - boxSize / 2f) / Chunk.Size,
                        blockSize, blockSize, UnityEngine.Color.red);
                    break;
                }
                case DrawingBrushType.Circle:
                {
                    DebugDraw.Circle(worldX / Chunk.Size - 0.5f, worldY / Chunk.Size - 0.5f, circleRadius / (float)Chunk.Size);
                    break;
                }
            }

            var r = chunk.Data.colors[blockIndexInChunk * 4];
            var g = chunk.Data.colors[blockIndexInChunk * 4 + 1];
            var b = chunk.Data.colors[blockIndexInChunk * 4 + 2];
            var a = chunk.Data.colors[blockIndexInChunk * 4 + 3];

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
                    Draw.Line(_lastPointDrawnForLine.Value.x, _lastPointDrawnForLine.Value.y,
                        blockPosition.x, blockPosition.y, (x, y) => DrawBrush(x, y, false));
            }
            else if (Input.GetMouseButton(0))
            {
                if (_lastPointDrawn == null)
                    DrawBrush(blockPosition.x, blockPosition.y);
                else
                    Draw.Line(_lastPointDrawn.Value.x, _lastPointDrawn.Value.y, blockPosition.x, blockPosition.y, (x, y) => DrawBrush(x, y, false));
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
            return new Vector2i((int) Mathf.Floor((worldPos.x + 0.5f) * Chunk.Size), (int) Mathf.Floor((worldPos.y + 0.5f) * 64.0f));
        }

        private void DrawBrush(int x, int y, bool immediate = true)
        {
            switch (selectedDrawingBrush)
            {
                case DrawingBrushType.Box:
                    Draw.Rectangle(x, y, boxSize, boxSize, (i, j) => PutBlock(i, j, immediate));
                    break;
                case DrawingBrushType.Circle:
                    Draw.Circle(x, y, circleRadius, (i, j) => PutBlock(i, j, immediate));
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

            var r = (byte)(blockColor.r / ((int) _currentLayer + 1.0f));
            var g = (byte)(blockColor.g / ((int)_currentLayer + 1.0f));
            var b = (byte)(blockColor.b / ((int)_currentLayer + 1.0f));
            if (selectedState == 1)
            {
                var fireSpread = BlockConstants.BlockDescriptors[selectedDrawBlock].FireSpreader;
                if (fireSpread != null && fireSpread.CombustionProbability > 0.0f)
                {
                    blockColor = fireSpread.FireColor;
                    blockColor.Shift(out r, out g, out b);
                }
            }

            var chunk = GetChunkFromWorld(worldX, worldY);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod(worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(worldY, Chunk.Size);

            if (colorizeOnly)
            {
                var i = blockYInChunk * Chunk.Size + blockXInChunk;
                chunk.Data.colors[i * 4] = (byte)(pixelColorOverride.r / ((int) _currentLayer + 1.0f));
                chunk.Data.colors[i * 4 + 1] = (byte)(pixelColorOverride.g / ((int) _currentLayer + 1.0f));
                chunk.Data.colors[i * 4 + 2] = (byte)(pixelColorOverride.b / ((int) _currentLayer + 1.0f));
                chunk.Data.colors[i * 4 + 3] = pixelColorOverride.a;
            }
            else
            {
                var descriptor = BlockConstants.BlockDescriptors[selectedDrawBlock];
                if (descriptor.InitialStates != 0 && selectedState == 0)
                    selectedState = BlockConstants.BlockDescriptors[selectedDrawBlock].InitialStates;
                chunk.PutBlock(blockXInChunk, blockYInChunk, selectedDrawBlock, r, g, b, blockColor.a,
                    selectedState, BlockConstants.BlockDescriptors[selectedDrawBlock].BaseHealth, 0);
                if (descriptor.PlantGrower != null)
                {
                    ref var plantBlockData = ref chunk.GetPlantBlockData(blockXInChunk, blockYInChunk, selectedDrawBlock);
                    if (plantBlockData.id != 0)
                        plantBlockData.Reset(selectedDrawBlock, BlockIdGenerator.Next());
                }
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
            var chunkPosition = new Vector2i((int) Mathf.Floor(worldX / Chunk.Size),
                (int) Mathf.Floor(worldY / Chunk.Size));
            return chunkLayers[(int) _currentLayer].ServerChunkMap.Contains(chunkPosition)
                ? chunkLayers[(int) _currentLayer].ServerChunkMap[chunkPosition]
                : null;
        }

        private Color GetBlockColor()
        {
            if (overrideDefaultColors)
                return new Color(pixelColorOverride.r, pixelColorOverride.g, pixelColorOverride.b, pixelColorOverride.a);

            var block = selectedDrawBlock;
            var color = BlockConstants.BlockDescriptors[block].Color;
            color.Shift(out var r, out var g, out var b);
            return new Color(r, g, b, color.a);
        }
    }
}
