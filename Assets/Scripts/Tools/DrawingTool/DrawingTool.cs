using System;
using Blocks;
using Chunks;
using Tiles;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Utils.Drawing;
using Color = Utils.Color;

namespace Tools.DrawingTool
{
    public class DrawingTool : MonoBehaviour
    {
        [HideInInspector]
        public DrawingParameters.DrawingParameters parameters;

        public bool drawBrushSelection;

        public bool colorizeOnly;
        public bool overrideDefaultColors;
        public Color32 pixelColorOverride;

        public WorldManager worldManager;

        public Text uiCoordText;

        private Vector2i? _lastPointDrawn;
        private Vector2i? _lastPointDrawnForLine;

        private readonly UniqueQueue<Vector2i> _blockQueue = new UniqueQueue<Vector2i>();

        private ChunkLayerType _currentLayer = ChunkLayerType.Foreground;

        private void Awake()
        {
            parameters = GetComponent<DrawingParameters.DrawingParameters>();
        }

        private void Update()
        {
            if (GlobalConfig.StaticGlobalConfig.disableDrawingTool)
                return;

            var blockPosition = GetWorldPositionFromMousePosition();

            if (Input.GetKey(KeyCode.LeftControl))
                _currentLayer = ChunkLayerType.Background;
            else
                _currentLayer = ChunkLayerType.Foreground;

            if (drawBrushSelection)
            {
                parameters.DrawBrush(blockPosition.x - Chunk.Size / 2f, blockPosition.y - Chunk.Size / 2f, 1f / Chunk.Size);
                PrintSelectedBlockInfo(blockPosition.x, blockPosition.y);
            }

            if (GlobalConfig.StaticGlobalConfig.outlineChunks)
            {
                DrawChunkGrid(blockPosition.x, blockPosition.y);
            }

            switch (parameters.tool)
            {
                case DrawingToolType.Brush:
                    UpdateBrush(blockPosition);
                    break;
                case DrawingToolType.Fill:
                    UpdateFill(blockPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawChunkGrid(float worldX, float worldY)
        {
            var chunk = worldManager.GetChunk(GetChunkPosition(worldX, worldY), _currentLayer);
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

        private void PrintSelectedBlockInfo(float worldX, float worldY)
        {
            var chunk = worldManager.GetChunk(GetChunkPosition(worldX, worldY), _currentLayer);
            if (chunk == null)
                return;
            var blockXInChunk = Helpers.Mod((int) worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod((int) worldY, Chunk.Size);
            var blockIndexInChunk = blockYInChunk * Chunk.Size + blockXInChunk;
            ref var block = ref chunk.GetBlockInfo(blockIndexInChunk);
            var r = chunk.Colors[blockIndexInChunk * 4];
            var g = chunk.Colors[blockIndexInChunk * 4 + 1];
            var b = chunk.Colors[blockIndexInChunk * 4 + 2];
            var a = chunk.Colors[blockIndexInChunk * 4 + 3];

            uiCoordText.text =
                $"X: {blockXInChunk}, Y: {blockYInChunk}\n"
                + $"Type: {BlockConstants.BlockNames[block.type]}\n"
                + $"StateBitset: {block.states}\n"
                + $"Health: {block.health}\n"
                + $"Lifetime: {block.lifetime}\n"
                + $"UpdatedFlag: {chunk.BlockUpdatedFlags[blockIndexInChunk]}\n"
                + $"Color: [{r},{g},{b},{a}]\n"
                + $"Chunk X:{chunk.Position.x}, Chunk Y: {chunk.Position.y}\n"
                + $"Current UpdatedFlag: {WorldManager.UpdatedFlag}";
        }

        private void UpdateBrush(Vector2i blockPosition)
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
                Draw.Fill(blockPosition.x, blockPosition.y, GetBlockType, (x, y) => PutBlock(x, y));
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
            return new Vector2i((int) Mathf.Floor((worldPos.x + 0.5f) * Chunk.Size), (int) Mathf.Floor((worldPos.y + 0.5f) * Chunk.Size));
        }

        private void DrawBrush(int x, int y, bool immediate = true)
        {
            switch (parameters.brush)
            {
                case DrawingBrushType.Box:
                    Draw.Rectangle(x, y, parameters.size, parameters.size, (i, j) => PutBlock(i, j, immediate));
                    break;
                case DrawingBrushType.Circle:
                    Draw.Circle(x, y, parameters.size, (i, j) => PutBlock(i, j, immediate));
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
            if (parameters.state == 1)
            {
                var fireSpread = BlockConstants.BlockDescriptors[parameters.block].FireSpreader;
                if (fireSpread != null && fireSpread.CombustionProbability > 0.0f)
                {
                    blockColor = fireSpread.FireColor;
                    blockColor.Shift(out r, out g, out b);
                }
            }

            var chunk = worldManager.GetChunk(GetChunkPosition(worldX, worldY), _currentLayer);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod(worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(worldY, Chunk.Size);

            if (colorizeOnly)
            {
                var i = blockYInChunk * Chunk.Size + blockXInChunk;
                chunk.Colors[i * 4] = (byte)(pixelColorOverride.r / ((int) _currentLayer + 1.0f));
                chunk.Colors[i * 4 + 1] = (byte)(pixelColorOverride.g / ((int) _currentLayer + 1.0f));
                chunk.Colors[i * 4 + 2] = (byte)(pixelColorOverride.b / ((int) _currentLayer + 1.0f));
                chunk.Colors[i * 4 + 3] = pixelColorOverride.a;
            }
            else
            {
                var descriptor = BlockConstants.BlockDescriptors[parameters.block];
                if (descriptor.InitialStates != 0 && parameters.state == 0)
                    parameters.state = BlockConstants.BlockDescriptors[parameters.block].InitialStates;
                chunk.PutBlock(blockXInChunk, blockYInChunk, parameters.block, r, g, b, blockColor.a,
                    parameters.state, BlockConstants.BlockDescriptors[parameters.block].BaseHealth, 0, 0);
                if (descriptor.PlantGrower != null)
                {
                    ref var plantBlockData = ref chunk.GetPlantBlockData(blockXInChunk, blockYInChunk, parameters.block);
                    if (plantBlockData.id != 0)
                        plantBlockData.Reset(parameters.block, UniqueIdGenerator.Next());
                }
            }

            worldManager.QueueChunkForReload(chunk.Position, _currentLayer);
        }

        private int GetBlockType(int worldX, int worldY)
        {
            var chunk = worldManager.GetChunk(GetChunkPosition(worldX, worldY), _currentLayer);
            if (chunk == null)
                return -1;

            var blockXInChunk = Helpers.Mod(worldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(worldY, Chunk.Size);

            return chunk.GetBlockType(blockYInChunk * Chunk.Size + blockXInChunk);
        }

        private Vector2i GetChunkPosition(float worldX, float worldY)
        {
            return new Vector2i((int) Mathf.Floor(worldX / Chunk.Size),
                (int) Mathf.Floor(worldY / Chunk.Size));
        }

        private Color GetBlockColor()
        {
            if (overrideDefaultColors)
                return new Color(pixelColorOverride.r, pixelColorOverride.g, pixelColorOverride.b, pixelColorOverride.a);

            var block = parameters.block;
            var color = BlockConstants.BlockDescriptors[block].Color;
            color.Shift(out var r, out var g, out var b);
            return new Color(r, g, b, color.a);
        }
    }
}
