using System;
using System.Diagnostics;
using Blocks;
using Chunks;
using Collision;
using NUnit.Framework;

namespace Tests
{
    public class TestCollisions
    {
        [Test]
        public void TestGenerateLines()
        {
            var grid = new int[8, 8]
            {
                {0, 0, 0, 0, 0, 0, 0, 0,},
                {0, 0, 0, 0, 0, 0, 0, 0,},
                {0, 0, 1, 1, 1, 1, 0, 0,},
                {0, 0, 1, 1, 1, 0, 0, 0,},
                {0, 0, 1, 0, 1, 1, 0, 0,},
                {0, 0, 1, 1, 1, 1, 0, 0,},
                {0, 0, 0, 0, 0, 0, 0, 0,},
                {0, 0, 0, 0, 0, 0, 0, 0,}
            };
            //   7   {0, 0, 0, 0, 0, 0, 0, 0,}
            //   6   {0, 0, 0, 0, 0, 0, 0, 0,},
            //   5   {0, 0, 0, 1, 1, 0, 0, 0,},
            //   4   {0, 0, 1, 1, 1, 1, 0, 0,},
            //   3   {0, 0, 1, 1, 0, 0, 0, 0,},
            //   2   {0, 0, 1, 1, 1, 0, 0, 0,},
            //   1   {0, 0, 0, 0, 0, 0, 1, 0.},
            //   0   {0, 0, 0, 0, 0, 0, 0, 0,},

            //        0  1  2  3  4  5  6  7
            ChunkCollision.GenerateLines(grid,
                out var horizontalLines,
                out var verticalLines);

            var cols = ChunkCollision.GenerateColliders(horizontalLines, verticalLines);
        }
    }
}
