using System.Collections.Generic;
using System.Linq;
using Blocks;
using Chunks.Server;
using UnityEngine;

namespace Chunks.Collision
{
    public static class ChunkCollision
    {
        public static List<List<Vector2>> ComputeChunkColliders(ChunkServer chunk)
        {
            var horizontalLines = new Dictionary<Vector2, Vector2>();
            var verticalLines = new Dictionary<Vector2, Vector2>();

            GenerateLines(chunk, horizontalLines, verticalLines);

            return GenerateColliders(horizontalLines, verticalLines);
        }

        private static void GenerateLines(ChunkServer chunk,
            IDictionary<Vector2, Vector2> horizontalLines,
            IDictionary<Vector2, Vector2> verticalLines)
        {
            var blocks = chunk.Blocks;
            var tmpReversedHorizontalLines = new Dictionary<Vector2, Vector2>();
            var tmpReversedVerticalLines = new Dictionary<Vector2, Vector2>();
            var tmpHorizontalLines = new Dictionary<Vector2, Vector2>();
            var tmpVerticalLines = new Dictionary<Vector2, Vector2>();

            Vector2 start = default, end = default;
            for (var y = 0; y < Chunk.Size; ++y) // lines
            {
                for (var x = 0; x < Chunk.Size; ++x) // columns
                {
                    if (!Collides(x, y, blocks)
                    || IsInsideADirtyRect(x, y, chunk.DirtyRects))
                        continue;

                    // Check left neighbour
                    if (x == 0)
                    {
                        start.Set(x, y);
                        end.Set(x, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedVerticalLines, ref tmpVerticalLines);
                    }
                    else if (!Collides(x - 1, y, blocks) || IsInsideADirtyRect(x - 1, y, chunk.DirtyRects))
                    {
                        start.Set(x, y);
                        end.Set(x, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedVerticalLines, ref tmpVerticalLines);
                    }

                    // Check right neighbour
                    if (x == Chunk.Size - 1)
                    {
                        start.Set(x + 1, y);
                        end.Set(x + 1, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedVerticalLines, ref tmpVerticalLines);
                    }
                    else if (!Collides(x + 1, y, blocks) || IsInsideADirtyRect(x + 1, y, chunk.DirtyRects))
                    {
                        start.Set(x + 1, y);
                        end.Set(x + 1, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedVerticalLines, ref tmpVerticalLines);
                    }

                    // Check down neighbour
                    if (y == 0)
                    {
                        start.Set(x, y);
                        end.Set(x + 1, y);
                        ExtendExistingOrAdd(start, end, ref tmpReversedHorizontalLines, ref tmpHorizontalLines);
                    }
                    else if (!Collides(x, y - 1, blocks) || IsInsideADirtyRect(x, y - 1, chunk.DirtyRects))
                    {
                        start.Set(x, y);
                        end.Set(x + 1, y);
                        ExtendExistingOrAdd(start, end, ref tmpReversedHorizontalLines, ref tmpHorizontalLines);
                    }

                    // Check up neighbour
                    if (y == Chunk.Size - 1)
                    {
                        start.Set(x, y + 1);
                        end.Set(x + 1, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedHorizontalLines, ref tmpHorizontalLines);
                    }
                    else if (!Collides(x, y + 1, blocks) || IsInsideADirtyRect(x, y + 1, chunk.DirtyRects))
                    {
                        start.Set(x, y + 1);
                        end.Set(x + 1, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedHorizontalLines, ref tmpHorizontalLines);
                    }
                }
            }

            foreach (var horizontalLine in tmpReversedHorizontalLines)
            {
                horizontalLines.Add(horizontalLine.Key, horizontalLine.Value);
                horizontalLines.Add(horizontalLine.Value, horizontalLine.Key);
            }

            foreach (var verticalLine in tmpReversedVerticalLines)
            {
                verticalLines.Add(verticalLine.Key, verticalLine.Value);
                verticalLines.Add(verticalLine.Value, verticalLine.Key);
            }
        }

        private static List<List<Vector2>> GenerateColliders(
            Dictionary<Vector2, Vector2> horizontalLines,
            Dictionary<Vector2, Vector2> verticalLines)
        {
            var colliderLists = new List<List<Vector2>>();
            var lines = new [] { horizontalLines, verticalLines };



            while (horizontalLines.Count > 0)
            {
                var idx = 0;
                var colliderPoints = new List<Vector2>();

                var line = lines[idx].ElementAt(0);
                var bluePoint = line.Key;
                var redPoint = line.Value;
                colliderPoints.Add(redPoint);
                lines[idx].Remove(bluePoint);
                lines[idx].Remove(redPoint);

                while (bluePoint != redPoint)
                {
                    idx = (idx + 1) % 2;
                    var greenPoint = redPoint;
                    redPoint = lines[idx][greenPoint];
                    lines[idx].Remove(greenPoint);
                    lines[idx].Remove(redPoint);
                    colliderPoints.Add(redPoint);
                }

                colliderLists.Add(colliderPoints);
            }

            return colliderLists;
        }

        private static bool Collides(int x, int y, Block[] blocks)
        {
            return BlockConstants.BlockDescriptors[blocks[y * Chunk.Size + x].type].Tag == BlockTags.Solid;
        }

        private static bool IsInsideADirtyRect(int x, int y, ChunkDirtyRect[] dirtyRects)
        {
            for (var i = 0; i < dirtyRects.Length; ++i)
            {
                if (dirtyRects[i].X < 0)
                    continue;

                var startX = ChunkServer.DirtyRectX[i] + dirtyRects[i].X;
                var startY = ChunkServer.DirtyRectY[i] + dirtyRects[i].Y;
                var endX = ChunkServer.DirtyRectX[i] + dirtyRects[i].XMax;
                var endY = ChunkServer.DirtyRectY[i] + dirtyRects[i].YMax;

                if (x >= startX && x <= endX && y >= startY && y <= endY)
                    return true;
            }

            return false;
        }

        private static void ExtendExistingOrAdd(Vector2 start, Vector2 end,
            ref Dictionary<Vector2, Vector2> reversedLines,
            ref Dictionary<Vector2, Vector2> lines)
        {
            lines.Add(start, end);

            if (reversedLines.ContainsKey(start))
            {
                var previousStart = reversedLines[start];
                lines.Remove(start);
                lines[previousStart] = end;

                reversedLines.Add(end, reversedLines[start]);
                reversedLines.Remove(start);

                if (lines.ContainsKey(end))
                {
                    reversedLines[lines[end]] = previousStart;
                    reversedLines.Remove(end);
                    lines[previousStart] = lines[end];
                    lines.Remove(end);
                }
            }
            else
            {
                // let's check if we can connect it to an existing line on the next block
                if (lines.ContainsKey(end))
                {
                    reversedLines[lines[end]] = start;

                    lines[start] = lines[end];
                    lines.Remove(end);
                }
                else
                    reversedLines.Add(end, start);
            }
        }
    }
}
