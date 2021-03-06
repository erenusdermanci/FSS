using System.Collections.Generic;
using System.Threading;
using DataComponents;
using MonoBehaviours;
using static Constants;

namespace ChunkTasks
{
    public class GenerationTask : ChunkTask
    {
        public ThreadLocal<ConfiguredNoise> HeightNoise;
        public ThreadLocal<ConfiguredNoise> Noise;
        private ConfiguredNoise _heightNoise;
        private ConfiguredNoise _noise;

        public GenerationTask(Chunk chunk) : base(chunk)
        {

        }

        protected override void Execute()
        {
            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            if (ProceduralGenerator.IsEnabled)
            {
                _noise = Noise.Value;
                _heightNoise = HeightNoise.Value;
                _heightNoise.Configure(ProceduralGenerator.StaticNoiseConfig.perlinConfigHeight);
                GenerateProcedurally();
            }
            else
                GenerateEmpty();

            for (var i = 0; i < Chunk.Size * Chunk.Size; i++)
                Chunk.BlockCounts[Chunk.blockData.types[i]] += 1;

            Chunk.Dirty = true;
        }

        private void GenerateProcedurally()
        {
            // First we need to determine the terrain noise, while taking into account
            // the position of the chunk -> we do not want any sky blocks inside the terrain and vice versa

            // Afterwards, we need to generate the noise for the terrain and the sky separately, using
            // their own thresholds.

            for (var x = 0; x < Chunk.Size; x++)
            {
                // Determine terrain verticality
                var vertNoise = _heightNoise.GetNoise(Chunk.Position.x + x, Chunk.Position.y);

                var vertIdx = (int)(vertNoise * Chunk.Size) - (int)(Chunk.Size * Chunk.Position.y);

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
        }

        private void GenerateEmpty()
        {
            for (var i = 0; i < Chunk.Size * Chunk.Size; i++)
            {
                Chunk.blockData.colors[i * 4] = BlockColors[(int) Blocks.Air].r;
                Chunk.blockData.colors[i * 4 + 1] = BlockColors[(int) Blocks.Air].g;
                Chunk.blockData.colors[i * 4 + 2] = BlockColors[(int) Blocks.Air].b;
                Chunk.blockData.colors[i * 4 + 3] = BlockColors[(int) Blocks.Air].a;
                Chunk.blockData.types[i] = (int) Blocks.Air;
            }
        }

        private void GenerateBlock(int x, int y, bool sky)
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

            var noise = _noise.GetNoise(Chunk.Position.x + (float)x / Chunk.Size,
                Chunk.Position.y + (float)y / Chunk.Size);

            var block = (int)GetBlockFromNoise(noise, sky);
            var blockColor = BlockColors[block];

            var i = y * Chunk.Size + x;

            Chunk.blockData.colors[i * 4] = blockColor.r;
            Chunk.blockData.colors[i * 4 + 1] = blockColor.g;
            Chunk.blockData.colors[i * 4 + 2] = blockColor.b;
            Chunk.blockData.colors[i * 4 + 3] = blockColor.a;
            Chunk.blockData.types[i] = block;
        }

        private static Blocks GetBlockFromNoise(float noise, bool sky = false)
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

            return Blocks.Border;
        }
    }
}