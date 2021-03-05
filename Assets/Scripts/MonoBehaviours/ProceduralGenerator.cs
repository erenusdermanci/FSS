using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonoBehaviours
{
    public class ProceduralGenerator : MonoBehaviour
    {
        public NoiseConfig noiseConfig;
        public static NoiseConfig StaticNoiseConfig;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticNoiseConfig = new NoiseConfig(noiseConfig);
        }

        private void Update()
        {
            if (StaticNoiseConfig.Equals(noiseConfig))
                return;
            StaticNoiseConfig = new NoiseConfig(noiseConfig);

            UpdateEvent?.Invoke(this, null);
        }

        [Serializable]
        public struct BlockThresholdStruct
        {
            public Constants.Blocks type;
            public float threshold;

            public BlockThresholdStruct(Constants.Blocks type, float threshold)
            {
                this.type = type;
                this.threshold = threshold;
            }
        }

        [Serializable]
        public struct PerlinConfig
        {
            public int octaves;
            public float gain;
            public float frequency;
            public float lacunarity;
            public float xOffset;
            public float yOffset;

            public PerlinConfig(PerlinConfig other)
            {
                octaves = other.octaves;
                gain = other.gain;
                frequency = other.frequency;
                lacunarity = other.lacunarity;
                xOffset = other.xOffset;
                yOffset = other.yOffset;
            }

            public bool Equals(PerlinConfig other)
            {
                return octaves == other.octaves &&
                   gain.EqualsEpsilon(other.gain) &&
                   frequency.EqualsEpsilon(other.frequency) &&
                   lacunarity.EqualsEpsilon(other.lacunarity) &&
                   xOffset.EqualsEpsilon(other.xOffset) &&
                   yOffset.EqualsEpsilon(other.yOffset);
            }
        }

        [Serializable]
        public struct NoiseConfig
        {
            public PerlinConfig perlinConfigHeight;
            public PerlinConfig perlinConfigTerrain;
            public PerlinConfig perlinConfigSky;

            public List<BlockThresholdStruct> blockThresholdsTerrain;
            public List<BlockThresholdStruct> blockThresholdsSky;

            public NoiseConfig(NoiseConfig other)
            {
                perlinConfigHeight = new PerlinConfig(other.perlinConfigHeight);
                perlinConfigTerrain = new PerlinConfig(other.perlinConfigTerrain);
                perlinConfigSky = new PerlinConfig(other.perlinConfigSky);

                blockThresholdsTerrain = new List<BlockThresholdStruct>();
                for (var i = 0; i < other.blockThresholdsTerrain.Count; i++)
                {
                    blockThresholdsTerrain.Add(new BlockThresholdStruct(
                        other.blockThresholdsTerrain[i].type,
                        other.blockThresholdsTerrain[i].threshold));
                }

                blockThresholdsSky = new List<BlockThresholdStruct>();
                for (var i = 0; i < other.blockThresholdsSky.Count; i++)
                {
                    blockThresholdsSky.Add(new BlockThresholdStruct(
                        other.blockThresholdsSky[i].type,
                        other.blockThresholdsSky[i].threshold));
                }
            }

            public bool Equals(NoiseConfig other)
            {
                if (blockThresholdsTerrain.Count == other.blockThresholdsTerrain.Count)
                {
                    for (var i = 0; i < blockThresholdsTerrain.Count; ++i)
                    {
                        if (!blockThresholdsTerrain[i].threshold.EqualsEpsilon(other.blockThresholdsTerrain[i].threshold))
                            return false;
                        if (blockThresholdsTerrain[i].type != other.blockThresholdsTerrain[i].type)
                            return false;
                    }
                }
                else
                    return false;

                if (blockThresholdsSky.Count == other.blockThresholdsSky.Count)
                {
                    for (var i = 0; i < blockThresholdsSky.Count; ++i)
                    {
                        if (!blockThresholdsSky[i].threshold.EqualsEpsilon(other.blockThresholdsSky[i].threshold))
                            return false;
                        if (blockThresholdsSky[i].type != other.blockThresholdsSky[i].type)
                            return false;
                    }
                }
                else
                    return false;

                return perlinConfigHeight.Equals(other.perlinConfigHeight)
                    && perlinConfigTerrain.Equals(other.perlinConfigTerrain)
                    && perlinConfigSky.Equals(other.perlinConfigSky);
            }
        }
    }

    public class ConfiguredNoise
    {
        private FastNoiseLite noise;
        private float xOffset;
        private float yOffset;

        public ConfiguredNoise()
        {
            noise = new FastNoiseLite();
        }

        public void Configure(ProceduralGenerator.PerlinConfig config)
        {
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetFractalOctaves(config.octaves);
            noise.SetFractalGain(config.gain);
            noise.SetFrequency(config.frequency);
            noise.SetFractalType(FastNoiseLite.FractalType.FBm);
            xOffset = config.xOffset;
            yOffset = config.yOffset;
        }

        public float GetNoise(float x, float y)
        {
            return noise.GetNoise(x + xOffset, y + yOffset);
        }
    }
}
