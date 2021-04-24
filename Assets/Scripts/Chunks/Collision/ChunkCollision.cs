using System.Collections.Generic;
using System.Linq;
using Blocks;
using Chunks.Client;
using Utils;

namespace Chunks.Collision
{
    public static class ChunkCollision
    {
        public static List<List<Vector2i>> ComputeChunkColliders(ChunkClient chunk)
        {
            return ComputeChunkColliders(chunk.Blocks);
        }

        private static List<List<Vector2i>> ComputeChunkColliders(Block[] blocks)
        {
            var horizontalLines = new Dictionary<Vector2i, Vector2i>();
            var verticalLines = new Dictionary<Vector2i, Vector2i>();

            GenerateLines(blocks, horizontalLines, verticalLines);

            return GenerateColliders(horizontalLines, verticalLines);
        }

        private static void GenerateLines(Block[] blocks,
            IDictionary<Vector2i, Vector2i> horizontalLines,
            IDictionary<Vector2i, Vector2i> verticalLines)
        {
            var tmpReversedHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpReversedVerticalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpHorizontalLines = new Dictionary<Vector2i, Vector2i>();
            var tmpVerticalLines = new Dictionary<Vector2i, Vector2i>();

            Vector2i start = default, end = default;
            for (var y = 0; y < Chunk.Size; ++y) // lines
            {
                for (var x = 0; x < Chunk.Size; ++x) // columns
                {
                    if (!Collides(y * Chunk.Size + x, blocks))
                        continue;

                    // Check left neighbour
                    if (x == 0)
                    {
                        start.Set(x, y);
                        end.Set(x, y + 1);
                        ExtendExistingOrAdd(start, end, ref tmpReversedVerticalLines, ref tmpVerticalLines);
                    }
                    else if (!Collides(y * Chunk.Size + (x - 1), blocks))
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
                    else if (!Collides(y * Chunk.Size + x + 1, blocks))
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
                    else if (!Collides((y - 1) * Chunk.Size + x, blocks))
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
                    else if (!Collides((y + 1) * Chunk.Size + x, blocks))
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

        private static List<List<Vector2i>> GenerateColliders(
            Dictionary<Vector2i, Vector2i> horizontalLines,
            Dictionary<Vector2i, Vector2i> verticalLines)
        {
            var colliderLists = new List<List<Vector2i>>();
            var lines = new [] { horizontalLines, verticalLines };

            while (horizontalLines.Count > 0)
            {
                var idx = 0;
                var colliderPoints = new List<Vector2i>();

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

        private static bool Collides(int index, Block[] blocks)
        {
            return BlockConstants.BlockDescriptors[blocks[index].type].Tag == BlockTags.Solid;
        }

        private static void ExtendExistingOrAdd(Vector2i start, Vector2i end,
            ref Dictionary<Vector2i, Vector2i> reversedLines,
            ref Dictionary<Vector2i, Vector2i> lines)
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
