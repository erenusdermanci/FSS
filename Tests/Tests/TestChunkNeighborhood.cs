using System;
using System.Diagnostics;
using Chunks;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void BenchmarkUpdateOutsideChunk()
        {
            var rng = new System.Random();
            var size = 4096;
            var xSamples = new int[size];
            var ySamples = new int[size];
            // generate valid position samples
            for (var i = 0; i < size; ++i)
            {
                xSamples[i] = rng.Next(-Chunk.Size, Chunk.Size * 2);
                ySamples[i] = rng.Next(-Chunk.Size, Chunk.Size * 2);
            }

            var sw = new Stopwatch();
            sw.Start();
            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    ChunkNeighborhood.UpdateOutsideChunk(ref xSamples[x], ref ySamples[y], out var chunkIndex);
                }
            }
            sw.Stop();
            Console.WriteLine("Took " + sw.ElapsedMilliseconds + " ms");
        }
    }
}