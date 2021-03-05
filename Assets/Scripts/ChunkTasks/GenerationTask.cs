using System.Collections.Generic;
using System.Threading;
using DataComponents;
using MonoBehaviours;
using UnityEngine;

namespace ChunkTasks
{
    public class GenerationTask : ChunkTask
    {
        public Vector2 ChunkPos;
        public byte[] BlockColors;
        public int[] BlockTypes;

        public ThreadLocal<ConfiguredNoise> HeightNoise;
        public ThreadLocal<ConfiguredNoise> Noise;
        private ConfiguredNoise _heightNoise;
        private ConfiguredNoise _noise;

        public GenerationTask(Chunk chunk) : base(chunk)
        {

        }

        protected override void Execute()
        {
            _heightNoise = HeightNoise.Value;
            _heightNoise.Configure(ProceduralGenerator.StaticNoiseConfig.perlinConfigHeight);
            _noise = Noise.Value;

            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            // First we need to determine the terrain noise, while taking into account
            // the position of the chunk -> we do not want any sky blocks inside the terrain and vice versa

            // Afterwards, we need to generate the noise for the terrain and the sky separately, using
            // their own thresholds.

            for (var x = 0; x < Chunk.Size; x++)
            {
                // Determine terrain verticality
                var vertNoise = _heightNoise.GetNoise(ChunkPos.x + x, ChunkPos.y);

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

        private void GenerateBlock(int x, int y, bool sky = false)
        {
            ProceduralGenerator.PerlinConfig config;
            if (sky)
            {
                config = ProceduralGenerator.StaticNoiseConfig.perlinConfigSky;
            }
            else
            {
                config = ProceduralGenerator.StaticNoiseConfig.perlinConfigTerrain;
            }

            _noise.Configure(config);

            var noise = _noise.GetNoise(ChunkPos.x + (float)x / Chunk.Size,
                ChunkPos.y + (float)y / Chunk.Size);

            int block = (int)GetBlockFromNoise(noise, sky);
            var blockColor = Constants.BlockColors[block];

            var i = y * Chunk.Size + x;

            BlockColors[i * 4] = blockColor.r;
            BlockColors[i * 4 + 1] = blockColor.g;
            BlockColors[i * 4 + 2] = blockColor.b;
            BlockColors[i * 4 + 3] = blockColor.a;
            BlockTypes[i] = block;

            Chunk.BlockCounts[block] += 1;
        }

        private Constants.Blocks GetBlockFromNoise(float noise, bool sky = false)
        {
            List<ProceduralGenerator.BlockThresholdStruct> thresholds;
            if (sky)
            {
                thresholds = ProceduralGenerator.StaticNoiseConfig.blockThresholdsSky;
            }
            else
            {
                thresholds = ProceduralGenerator.StaticNoiseConfig.blockThresholdsTerrain;
            }

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (noise <= thresholds[i].threshold)
                    return thresholds[i].type;
            }

            return Constants.Blocks.Border;
        }
    }
}