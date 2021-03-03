using DataComponents;
using MonoBehaviours;
using System.Collections.Generic;
using UnityEngine;

namespace ChunkTasks
{
    public class GenerationTask : ChunkTask
    {
        public Vector2 ChunkPos;
        public byte[] BlockColors;
        public int[] BlockTypes;

        public GenerationTask(Chunk chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            for (var i = 0; i < BlockCounts.Length; ++i)
                BlockCounts[i] = 0;

            // First we need to determine the terrain noise, while taking into account
            // the position of the chunk -> we do not want any sky blocks inside the terrain and vice versa

            // Afterwards, we need to generate the noise for the terrain and the sky separately, using
            // their own thresholds.

            for (var x = 0; x < Chunk.Size; x++)
            {
                // Determine terrain verticality
                // TODO: Use simplex or fractal perlin noise: 1-dimensional and linear
                var vertNoise = ProceduralGenerator.OctavePerlin(ChunkPos.x + x, ChunkPos.y);

                var vertIdx = (int)(vertNoise * Chunk.Size) - (int)(Chunk.Size * ChunkPos.y);

                if (vertIdx > Chunk.Size - 1)
                {
                    // Totally terrain
                    for (var y = 0; y < Chunk.Size; y++)
                    {
                        GenerateBlock(x, y, false);
                    }
                }
                else if (vertIdx < 0)
                {
                    // Totally sky
                    for (var y = 0; y < Chunk.Size; y++)
                    {
                        GenerateBlock(x, y, true);
                    }
                }
                else
                {
                    for (var y = 0; y < Chunk.Size; y++)
                    {
                        GenerateBlock(x, y, y > vertIdx);
                    }
                }
            }

            Chunk.Dirty = true;
        }

        protected void Execute2()
        {
            for (var i = 0; i < BlockCounts.Length; ++i)
                BlockCounts[i] = 0;

            // First we need to determine the terrain noise, while taking into account
            // the position of the chunk -> we do not want any sky blocks inside the terrain and vice versa

            // Afterwards, we need to generate the noise for the terrain and the sky separately, using
            // their own thresholds.

            for (var gx = 0; gx < Chunk.Size; gx++)
            {
                var verticalityNoise = ProceduralGenerator.OctavePerlin(ChunkPos.x + (float)gx, ChunkPos.y);

                var verticalIdx = (int)(verticalityNoise * Chunk.Size);
                verticalIdx -= (int)(Chunk.Size * ChunkPos.y);

                if (verticalIdx > Chunk.Size - 1)
                {
                    // Fully sky
                }
                else if (verticalIdx < 0)
                {
                    // Fully terrain
                    for (var i = 0; i < Chunk.Size * Chunk.Size; ++i)
                    {
                        var y = i / Chunk.Size;
                        var x = i % Chunk.Size;
                        var noise = ProceduralGenerator.OctavePerlin(ChunkPos.x + (float)x / Chunk.Size,
                            ChunkPos.y + (float)y / Chunk.Size);
                        int block = (int)GetBlockFromNoise(noise);

                        var blockColor = Constants.BlockColors[block];
                        BlockColors[i * 4] = blockColor.r;
                        BlockColors[i * 4 + 1] = blockColor.g;
                        BlockColors[i * 4 + 2] = blockColor.b;
                        BlockColors[i * 4 + 3] = blockColor.a;
                        BlockTypes[i] = block;

                        BlockCounts[block] += 1;
                    }
                }
                else
                {
                    var i = verticalIdx * Chunk.Size + gx;
                    BlockTypes[i] = (int)Constants.Blocks.Border;
                    var blockColor = Constants.BlockColors[(int)Constants.Blocks.Border];
                    BlockColors[i * 4] = blockColor.r;
                    BlockColors[i * 4 + 1] = blockColor.g;
                    BlockColors[i * 4 + 2] = blockColor.b;
                    BlockColors[i * 4 + 3] = blockColor.a;
                }
            }

            Chunk.Dirty = true;
        }

        private void GenerateBlock(int x, int y, bool sky = false)
        {
            // TODO: Separate noise config for sky and cloud
            var noise = ProceduralGenerator.OctavePerlin(ChunkPos.x + (float)x / Chunk.Size,
                ChunkPos.y + (float)y / Chunk.Size);

            int block = (int)GetBlockFromNoise(noise, sky);
            var blockColor = Constants.BlockColors[block];

            var i = y * Chunk.Size + x;

            BlockColors[i * 4] = blockColor.r;
            BlockColors[i * 4 + 1] = blockColor.g;
            BlockColors[i * 4 + 2] = blockColor.b;
            BlockColors[i * 4 + 3] = blockColor.a;
            BlockTypes[i] = block;

            BlockCounts[block] += 1;
        }

        private Constants.Blocks GetBlockFromNoise(float noise, bool sky = false)
        {
            List<ProceduralGenerator.BlockThresholdStruct> thresholds;
            if (sky)
                thresholds = ProceduralGenerator.StaticNoiseConfig.blockThresholdsSky;
            else
                thresholds = ProceduralGenerator.StaticNoiseConfig.blockThresholdsTerrain;

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (noise <= thresholds[i].threshold)
                    return thresholds[i].type;
            }

            return Constants.Blocks.Border;
        }
    }
}