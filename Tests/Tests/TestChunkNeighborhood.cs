using System;
using System.Diagnostics;
using Blocks;
using Chunks;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        private readonly ChunkMap<ChunkServer> _chunkMap = new ChunkMap<ChunkServer>();
        private ChunkNeighborhood _chunkNeighborhood;

        [SetUp]
        public void Setup()
        {
            var chunk = new ChunkServer();
            chunk.Initialize();
            chunk.GenerateEmpty();
            var airColor = BlockConstants.BlockDescriptors[BlockConstants.Air].Color;
            for (var i = 0; i < Chunk.Size * Chunk.Size; i++)
            {
                chunk.Data.colors[i * 4] = airColor.r;
                chunk.Data.colors[i * 4 + 1] = airColor.g;
                chunk.Data.colors[i * 4 + 2] = airColor.b;
                chunk.Data.colors[i * 4 + 3] = airColor.a;
                chunk.Data.types[i] = BlockConstants.Air;
                chunk.Data.stateBitsets[i] = 0;
                chunk.Data.healths[i] = BlockConstants.BlockDescriptors[BlockConstants.Air].BaseHealth;
                chunk.Data.lifetimes[i] = 0;
            }

            _chunkNeighborhood = new ChunkNeighborhood(_chunkMap, chunk, new Random());
        }

        [Test]
        public void BenchmarkUpdateOutsideChunk()
        {
            var rng = new Random();
            const int size = 4096;
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
                    ChunkNeighborhood.UpdateOutsideChunk(ref xSamples[x], ref ySamples[y], out _);
                }
            }

            sw.Stop();
            Console.WriteLine("Took " + sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void BenchmarkUpdateDirtyRectForAdjacentBlock()
        {
            var rng = new Random();
            const int size = 4096;
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
                    _chunkNeighborhood.UpdateDirtyRectForAdjacentBlock(xSamples[x], ySamples[y]);
                }
            }

            sw.Stop();
            Console.WriteLine("Took " + sw.ElapsedMilliseconds + " ms");
        }

        [Test]
        public void BenchmarkUpdateAdjacentBlockDirty()
        {
            var rng = new Random();
            const int size = 4096;
            var xSamples = new int[size];
            var ySamples = new int[size];
            // generate valid position samples
            for (var i = 0; i < size; ++i)
            {
                xSamples[i] = rng.Next(-Chunk.Size + 1, Chunk.Size * 2 - 1);
                ySamples[i] = rng.Next(-Chunk.Size + 1, Chunk.Size * 2 - 1);
            }

            var sw = new Stopwatch();
            sw.Start();
            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    _chunkNeighborhood.UpdateAdjacentBlockDirty(xSamples[x], ySamples[y]);
                }
            }

            sw.Stop();
            Console.WriteLine("Took " + sw.ElapsedMilliseconds + " ms");
        }
    }
}