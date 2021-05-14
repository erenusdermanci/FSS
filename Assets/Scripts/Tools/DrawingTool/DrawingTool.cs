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

        private void Awake()
        {
            parameters = GetComponent<DrawingParameters.DrawingParameters>();
        }

        private void Update()
        {
            if (GlobalConfig.StaticGlobalConfig.disableDrawingTool)
                return;

            Vector2i blockPosition;
            if (drawBrushSelection)
            {
                blockPosition = GetWorldBlockPositionFromMousePosition();
                parameters.DrawBrush(blockPosition.x - Chunk.Size / 2f, blockPosition.y - Chunk.Size / 2f, 1f / Chunk.Size);
                PrintSelectedBlockInfo(blockPosition.x, blockPosition.y);
            }

            var chunkManagerPosition = worldManager.transform.position;
            var worldTileSize = worldManager.tileGridThickness * 2 + 1;
            var worldWidth = worldTileSize * Tile.HorizontalChunks;
            var worldHeight = worldTileSize * Tile.VerticalChunks;
            var x = chunkManagerPosition.x - worldWidth / 2.0f;
            var y = chunkManagerPosition.y - worldHeight / 2.0f;

            var mousePos = Input.mousePosition;
            if (Camera.main is null)
            {
                throw new InvalidOperationException();
            }
            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            blockPosition = new Vector2i(
                (int) Mathf.Floor((worldPos.x - x + 0.5f) * Chunk.Size),
                (int) Mathf.Floor((worldPos.y - y + 0.5f) * Chunk.Size)
                );

            switch (parameters.tool)
            {
                case DrawingToolType.Brush:
                    UpdateBrush(blockPosition);
                    break;
                case DrawingToolType.Fill:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void PrintSelectedBlockInfo(float worldX, float worldY)
        {
            // var chunk = worldManager.GetChunk(GetChunkPosition(worldX, worldY));
            // if (chunk == null)
            //     return;
            // var blockXInChunk = Helpers.Mod((int) worldX, Chunk.Size);
            // var blockYInChunk = Helpers.Mod((int) worldY, Chunk.Size);
            // var blockIndexInChunk = blockYInChunk * Chunk.Size + blockXInChunk;
            // ref var block = ref chunk.GetBlockInfo(blockIndexInChunk);
            // var r = chunk.Colors[blockIndexInChunk * 4];
            // var g = chunk.Colors[blockIndexInChunk * 4 + 1];
            // var b = chunk.Colors[blockIndexInChunk * 4 + 2];
            // var a = chunk.Colors[blockIndexInChunk * 4 + 3];
            //
            // uiCoordText.text =
            //     $"X: {blockXInChunk}, Y: {blockYInChunk}\n"
            //     + $"Type: {BlockConstants.BlockNames[block.type]}\n"
            //     + $"StateBitset: {block.states}\n"
            //     + $"Health: {block.health}\n"
            //     + $"Lifetime: {block.lifetime}\n"
            //     + $"UpdatedFlag: {chunk.LastBlockUpdateFrame[blockIndexInChunk]}\n"
            //     + $"Color: [{r},{g},{b},{a}]\n"
            //     + $"Chunk X:{chunk.Position.x}, Chunk Y: {chunk.Position.y}\n"
            //     + $"Current Frame: {WorldManager.CurrentFrame}";
        }

        private void UpdateBrush(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_lastPointDrawnForLine != null && Input.GetKey(KeyCode.LeftShift))
                    Draw.Line(_lastPointDrawnForLine.Value.x, _lastPointDrawnForLine.Value.y,
                        blockPosition.x, blockPosition.y, (x, y) => DrawBrush(x, y));
            }
            else if (Input.GetMouseButton(0))
            {
                if (_lastPointDrawn == null)
                    DrawBrush(blockPosition.x, blockPosition.y);
                else
                    Draw.Line(_lastPointDrawn.Value.x, _lastPointDrawn.Value.y, blockPosition.x, blockPosition.y, (x, y) => DrawBrush(x, y));
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

        private Vector2i GetWorldBlockPositionFromMousePosition()
        {
            var mousePos = Input.mousePosition;
            if (Camera.main is null)
            {
                throw new InvalidOperationException();
            }

            var worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            var worldBlockPosition = new Vector2i(
                (int) Mathf.Floor((worldPos.x + 0.5f) * Chunk.Size),
                (int) Mathf.Floor((worldPos.y + 0.5f) * Chunk.Size)
            );
            return worldBlockPosition;
        }

        private void DrawBrush(int x, int y)
        {
            switch (parameters.brush)
            {
                case DrawingBrushType.Box:
                    DrawBlockRectangle(x - parameters.size / 2, y - parameters.size / 2, parameters.size, parameters.size);
                    break;
                case DrawingBrushType.Circle:
                    // Draw.Circle(x, y, parameters.size, (i, j) => PutBlock(i, j, immediate));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawBlockRectangle(int worldX, int worldY, int width, int height)
        {
            var blockColor = BlockConstants.BlockDescriptors[parameters.block].Color;

            if (parameters.state == 1)
            {
                var fireSpread = BlockConstants.BlockDescriptors[parameters.block].FireSpreader;
                if (fireSpread != null && fireSpread.CombustionProbability > 0.0f)
                {
                    blockColor = fireSpread.FireColor;
                }
            }

            worldManager.DrawRect(
                worldX, worldY, width, height,
                parameters.block, blockColor,
                parameters.state, 0
            );
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
