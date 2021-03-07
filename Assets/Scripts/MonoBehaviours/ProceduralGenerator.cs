using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace MonoBehaviours
{
    public class ProceduralGenerator : MonoBehaviour
    {
        public bool Enabled;
        public static bool IsEnabled;
        
        public GenerationConfig generationConfig;
        public static GenerationConfig StaticGenerationConfig;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticGenerationConfig = new GenerationConfig(generationConfig);

            IsEnabled = Enabled;
        }

        private void Update()
        {
            if (StaticGenerationConfig.Equals(generationConfig))
                return;
            StaticGenerationConfig = new GenerationConfig(generationConfig);

            IsEnabled = Enabled;

            UpdateEvent?.Invoke(this, null);
        }

        [Serializable]
        public struct BlockThresholdStruct
        {
            public BlockConstants.Blocks type;
            public float threshold;

            public BlockThresholdStruct(BlockConstants.Blocks type, float threshold)
            {
                this.type = type;
                this.threshold = threshold;
            }
        }

        [Serializable]
        public struct NoiseConfig
        {
            public FastNoiseLite.NoiseType type;
            public FastNoiseLite.FractalType fractalType;
            public int octaves;
            public float gain;
            public float frequency;
            public float lacunarity;
            public float xAmplitude;
            public float yAmplitude;
            public float xOffset;
            public float yOffset;

            public NoiseConfig(NoiseConfig other)
            {
                type = other.type;
                fractalType = other.fractalType;
                octaves = other.octaves;
                gain = other.gain;
                frequency = other.frequency;
                lacunarity = other.lacunarity;
                xAmplitude = other.xAmplitude;
                yAmplitude = other.yAmplitude;
                xOffset = other.xOffset;
                yOffset = other.yOffset;
            }

            public bool Equals(NoiseConfig other)
            {
                return type == other.type &&
                   fractalType == other.fractalType &&
                   octaves == other.octaves &&
                   gain.EqualsEpsilon(other.gain) &&
                   frequency.EqualsEpsilon(other.frequency) &&
                   lacunarity.EqualsEpsilon(other.lacunarity) &&
                   xAmplitude.EqualsEpsilon(other.xAmplitude) &&
                   yAmplitude.EqualsEpsilon(other.yAmplitude) &&
                   xOffset.EqualsEpsilon(other.xOffset) &&
                   yOffset.EqualsEpsilon(other.yOffset);
            }
        }

        [Serializable]
        public struct GenerationConfig
        {
            public NoiseConfig[] noiseConfigs;

            public List<BlockThresholdStruct> blockThresholdsTerrain;
            public List<BlockThresholdStruct> blockThresholdsSky;

            public GenerationConfig(GenerationConfig other)
            {
                noiseConfigs = new NoiseConfig[other.noiseConfigs.Length];
                for (var i = 0; i < noiseConfigs.Length; i++)
                    noiseConfigs[i] = new NoiseConfig(other.noiseConfigs[i]);

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

            public bool Equals(GenerationConfig other)
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

                if (noiseConfigs.Length == other.noiseConfigs.Length)
                {
                    for (var i = 0; i < noiseConfigs.Length; ++i)
                    {
                        if (!noiseConfigs[i].Equals(other.noiseConfigs[i]))
                            return false;
                    }
                }
                else
                    return false;

                return true;
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

        public void Configure(ProceduralGenerator.NoiseConfig config)
        {
            noise.SetNoiseType(config.type);
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
