using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using Chunks;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

namespace Collision
{
    public static class ChunkCollision
    {
        public static int[,] ComputeChunkColliders(ChunkServer chunk)
        {
            // non-optimized
            var isoValueData = new int[Chunk.Size, Chunk.Size];

            // fill iso-value threhsold array
            for (var y = 0; y < Chunk.Size; ++y)
            {
                for (var x = 0; x < Chunk.Size; ++x)
                {
                    if (BlockConstants.BlockDescriptors[chunk.Data.types[y * Chunk.Size + x]].Tag == BlockTags.Solid)
                    {
                        isoValueData[y, x] = 1;
                    }
                }
            }

            return isoValueData;
        }

        public static void GenerateLines(int[,] grid,
            out Dictionary<Vector2i, Vector2i> horizontalLines,
            out Dictionary<Vector2i, Vector2i> verticalLines)
        {
            var yLen = grid.GetLength(0);
            var xLen = grid.GetLength(1);
            // key is end pos, value is starting pos
            horizontalLines = new Dictionary<Vector2i, Vector2i>();
            verticalLines = new Dictionary<Vector2i, Vector2i>();

            var tmpReversedHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpReversedVerticalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpVerticalLines = new Dictionary<Vector2i, Vector2i>();

            for (var y = 0; y < yLen; ++y) // lines
            {
                for (var x = 0; x < xLen; ++x) // columns
                {
                    if (grid[y, x] == 1) // solid block
                    {
                        // Check left neighbour
                        if (x == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }
                        else if (grid[y, x - 1] == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }

                        // Check right neighbour
                        if (x == xLen - 1)
                        {
                            ExtendExistingOrAdd(new Vector2i(x + 1, y), new Vector2i(x + 1, y + 1),
                            ref tmpReversedVerticalLines,
                            ref tmpVerticalLines);
                        }
                        else if (grid[y, x + 1] == 0)
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
                        else if (grid[y - 1, x] == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y), new Vector2i(x + 1, y),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }

                        // Check up neighbour
                        if (y == yLen - 1)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y + 1), new Vector2i(x + 1, y + 1),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }
                        else if (grid[y + 1, x] == 0)
                        {
                            ExtendExistingOrAdd(new Vector2i(x, y + 1), new Vector2i(x + 1, y + 1),
                            ref tmpReversedHorizontalLines,
                            ref tmpHorizontalLines);
                        }
                    }
                }
            }



            foreach (var ln in tmpReversedHorizontalLines)
            {
                Debug.DrawLine(new Vector2(ln.Value.x, ln.Value.y), new Vector2(ln.Key.x, ln.Key.y), Color.white);
            }

            foreach (var ln in tmpReversedVerticalLines)
            {
                Debug.DrawLine(new Vector2(ln.Value.x, ln.Value.y), new Vector2(ln.Key.x, ln.Key.y), Color.white);
            }


            foreach (var horizontalLine in tmpReversedHorizontalLines)
            {
                try
                {
                    horizontalLines.Add(horizontalLine.Key, horizontalLine.Value);
                }
                catch (Exception)
                {
                    Debug.Log("TONTONH1");
                    Debug.DrawLine(new Vector2(horizontalLine.Value.x, horizontalLine.Value.y),
                        new Vector2(horizontalLine.Key.x, horizontalLine.Key.y), Color.red);
                    Debug.DrawLine(
                        new Vector2(horizontalLines[horizontalLine.Key].x, horizontalLines[horizontalLine.Key].y),
                        new Vector2(horizontalLine.Key.x, horizontalLine.Key.y), Color.blue);
                }

                try
                {
                    horizontalLines.Add(horizontalLine.Value, horizontalLine.Key);
                }
                catch (Exception)
                {
                    Debug.Log($"key is: {horizontalLine.Key} and val is: {horizontalLine.Value}");
                    Debug.DrawLine(new Vector2(horizontalLine.Key.x, horizontalLine.Key.y),
                        new Vector2(horizontalLine.Value.x, horizontalLine.Value.y), Color.red);
                    Debug.DrawLine(
                        new Vector2(horizontalLines[horizontalLine.Value].x, horizontalLines[horizontalLine.Value].y),
                        new Vector2(horizontalLine.Value.x, horizontalLine.Value.y), Color.blue);

                }

            }

            foreach (var verticalLine in tmpReversedVerticalLines)
            {
                try
                {
                    verticalLines.Add(verticalLine.Key, verticalLine.Value);
                }
                catch (Exception)
                {
                    Debug.Log("TONTONV1");
                    Debug.DrawLine(new Vector2(verticalLine.Value.x, verticalLine.Value.y),
                        new Vector2(verticalLine.Key.x, verticalLine.Key.y), Color.red);
                    Debug.DrawLine(
                        new Vector2(verticalLines[verticalLine.Key].x, verticalLines[verticalLine.Key].y),
                        new Vector2(verticalLine.Key.x, verticalLine.Key.y), Color.blue);

                }

                try
                {
                    verticalLines.Add(verticalLine.Value, verticalLine.Key);
                }
                catch (Exception)
                {
                    Debug.Log("TONTONV2");
                    Debug.DrawLine(new Vector2(verticalLine.Key.x, verticalLine.Key.y),
                        new Vector2(verticalLine.Value.x, verticalLine.Value.y), Color.red);
                    Debug.DrawLine(
                        new Vector2(verticalLines[verticalLine.Value].x, verticalLines[verticalLine.Value].y),
                        new Vector2(verticalLine.Value.x, verticalLine.Value.y), Color.blue);

                }
            }
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

            // the current bug is that we get more horizontal lines than we should have, which gets
            // > verticalLines so we run out of vertLine to use while traversing

            // if (reversedLines.ContainsKey(end) && lines.ContainsKey(reversedLines[end]))
            //     lines[reversedLines[end]] = end;

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
    }
}
