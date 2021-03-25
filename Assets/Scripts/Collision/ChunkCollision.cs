using System.Collections.Generic;
using System.Linq;
using Blocks;
using Chunks;
using UnityEngine;
using Utils;

namespace Collision
{
    public static class ChunkCollision
    {
        public static List<List<Vector2i>> ComputeChunkColliders(ChunkClient chunk)
        {
            return ComputeChunkColliders(chunk.Types);
        }

        public static List<List<Vector2i>> ComputeChunkColliders(ChunkServer chunk)
        {
            return ComputeChunkColliders(chunk.Data.types);
        }

        private static List<List<Vector2i>> ComputeChunkColliders(int[] grid)
        {
            GenerateLines(grid,
                out var horizontalLines,
                out var verticalLines);

            return GenerateColliders(horizontalLines, verticalLines);
        }

        public static void GenerateLines(int[] grid,
            out Dictionary<Vector2i, Vector2i> horizontalLines,
            out Dictionary<Vector2i, Vector2i> verticalLines)
        {
            horizontalLines = new Dictionary<Vector2i, Vector2i>();
            verticalLines = new Dictionary<Vector2i, Vector2i>();

            var tmpReversedHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpReversedVerticalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpVerticalLines = new Dictionary<Vector2i, Vector2i>();

            for (var y = 0; y < Chunk.Size; ++y) // lines
            {
                for (var x = 0; x < Chunk.Size; ++x) // columns
                {
                    if (IsSolid(grid[y * Chunk.Size + x]))
                    {
                        // Check left neighbour
                        if (x == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }
                        else if (!IsSolid(grid[y * Chunk.Size + (x - 1)]))
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }

                        // Check right neighbour
                        if (x == Chunk.Size - 1)
                        {
                            ExtendExistingOrAdd(new Vector2i(x + 1, y), new Vector2i(x + 1, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }
                        else if (!IsSolid(grid[y * Chunk.Size + (x + 1)]))
                        {
                            ExtendExistingOrAdd(new Vector2i(x + 1, y), new Vector2i(x + 1, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }

                        // Check down neighbour
                        if (y == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x + 1, y),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }
                        else if (!IsSolid(grid[(y - 1) * Chunk.Size + x]))
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x + 1, y),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }

                        // Check up neighbour
                        if (y == Chunk.Size - 1)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y + 1), new Vector2i(x + 1, y + 1),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }
                        else if (!IsSolid(grid[(y + 1) * Chunk.Size + x]))
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y + 1), new Vector2i(x + 1, y + 1),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }
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

        public static List<List<Vector2i>> GenerateColliders(
            Dictionary<Vector2i, Vector2i> horizontalLines,
            Dictionary<Vector2i, Vector2i> verticalLines)
        {
            var colliderLists = new List<List<Vector2i>>();
            // 0 is horizontal
            // 1 is vertical
            var lines = new [] {horizontalLines, verticalLines};

            while (horizontalLines.Count > 0)
            {
                var idx = 0;
                var colliderPoints = new List<Vector2i>();

                var line = lines[idx].ElementAt(0);
                var bluePoint = line.Key;
                var redPoint = line.Value;
                colliderPoints.Add(redPoint);
                lines[idx].Remove(bluePoint);
                lines[idx].Remove(redPoint); // ?

                while (bluePoint != redPoint)
                {
                    idx = (idx + 1) % 2;
                    var greenPoint = redPoint;
                    redPoint = lines[idx][greenPoint]; // we cannot find our next redpoint if the shape is _-_
                    lines[idx].Remove(greenPoint);
                    lines[idx].Remove(redPoint);
                    colliderPoints.Add(redPoint);
                }

                colliderLists.Add(colliderPoints);
            }

            return colliderLists;
        }

        private static bool IsSolid(int type)
        {
            var isSolid = BlockConstants.BlockDescriptors[type].Tag == BlockTags.Solid;
            return isSolid;
        }

        private static void ExtendExistingOrAdd(Vector2i start, Vector2i end,
            ref Dictionary<Vector2i, Vector2i> reversedLines,
            ref Dictionary<Vector2i, Vector2i> lines)
        {
            // we might want to keep another hashmap and extend from the opposite side

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
                    // let's extend that existing line all the way here, making sure we keep its end but
                    // adjust its start
                    // if my end is the key to a start line,
                    // i want to modify my
                    reversedLines[lines[end]] = previousStart; // on dirait que je ne prends pas tout le bout
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
                    // let's extend that existing line all the way here, making sure we keep its end but
                    // adjust its start
                    // if my end is the key to a start line,
                    // i want to modify my
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
