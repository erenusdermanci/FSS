using System;
using System.Collections.Generic;
using System.Threading;
using DataComponents;
using MonoBehaviours;
using UnityEngine;
using Utils;
using static BlockConstants;

namespace ChunkTasks
{
    public class GenerationTask : ChunkTask
    {
        public ThreadLocal<System.Random> Rng;
        public ThreadLocal<ConfiguredNoise[]> Noises;
        private System.Random _rng;
        private ConfiguredNoise[] _noises;

        public GenerationTask(Chunk chunk) : base(chunk)
        {

        }

        protected override void Execute()
        {
            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            if (ProceduralGenerator.IsEnabled)
            {
                _rng = Rng.Value;

                _noises = new ConfiguredNoise[Noises.Value.Length];
                _noises[0] = Noises.Value[0]; // Height 1
                _noises[1] = Noises.Value[1]; // Height 2
                _noises[2] = Noises.Value[2]; // Terrain/Sky

                _noises[0].Configure(ProceduralGenerator.StaticGenerationConfig.noiseConfigs[0]);
                _noises[1].Configure(ProceduralGenerator.StaticGenerationConfig.noiseConfigs[1]);
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

            // Config specific to height:
            var xAmplitude1 = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[0].xAmplitude;
            var yAmplitude1 = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[0].yAmplitude;
            var xAmplitude2 = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[1].xAmplitude;
            var yAmplitude2 = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[1].yAmplitude;

            for (var x = 0; x < Chunk.Size; x++)
            {
                // Determine terrain verticality
                var vertNoise1 = _noises[0].GetNoise((float)((Chunk.Position.x * Chunk.Size) + x) / (Chunk.Size * xAmplitude1),0);
                var vertNoise2 = _noises[1].GetNoise((float)((Chunk.Position.x * Chunk.Size) + x) / (Chunk.Size * xAmplitude2), 0);

                var vertNoiseAcc = (vertNoise1 + vertNoise2) / 2;
                var vertIdx = (vertNoiseAcc * Chunk.Size * yAmplitude1 * yAmplitude2);
                var separator = Chunk.Position.y * Chunk.Size;

                for (var y = 0; y < Chunk.Size; y++)
                {
                    if (separator + y <= vertIdx)
                    {
                        GenerateBlock(x, y, false);
                    }
                    else
                    {
                        GenerateBlock(x, y, true);
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
            ProceduralGenerator.NoiseConfig config;
            if (sky)
            {
                config = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[3];
            }
            else
            {
                config = ProceduralGenerator.StaticGenerationConfig.noiseConfigs[2];
            }

            _noises[2].Configure(config);

            var noise = _noises[2].GetNoise(Chunk.Position.x + (float)x / Chunk.Size,
                Chunk.Position.y + (float)y / Chunk.Size);

            var block = (int)GetBlockFromNoise(noise, sky);
            var blockColor = BlockColors[block];

            var i = y * Chunk.Size + x;

            var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockColorMaxShift[block]);
            Chunk.blockData.colors[i * 4] = Helpers.ShiftColorComponent(blockColor.r, shiftAmount);
            Chunk.blockData.colors[i * 4 + 1] = Helpers.ShiftColorComponent(blockColor.g, shiftAmount);
            Chunk.blockData.colors[i * 4 + 2] = Helpers.ShiftColorComponent(blockColor.b, shiftAmount);
            Chunk.blockData.colors[i * 4 + 3] = blockColor.a;
            Chunk.blockData.types[i] = block;
        }

        private static Blocks GetBlockFromNoise(float noise, bool sky = false)
        {
            List<ProceduralGenerator.BlockThresholdStruct> thresholds;
            if (sky)
            {
                thresholds = ProceduralGenerator.StaticGenerationConfig.blockThresholdsSky;
            }
            else
            {
                thresholds = ProceduralGenerator.StaticGenerationConfig.blockThresholdsTerrain;
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