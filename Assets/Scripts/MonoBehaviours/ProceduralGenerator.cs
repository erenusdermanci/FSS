using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonoBehaviours
{
    public class ProceduralGenerator : MonoBehaviour
    {
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
        public struct NoiseConfig
        {
            public int octaves;
            public float persistence;
            public float frequency;
            public float amplitude;
            public float frequencyMultiplier;
            public float xOffset;
            public float yOffset;
            public List<BlockThresholdStruct> blockThresholdsTerrain;
            public List<BlockThresholdStruct> blockThresholdsSky;

            public NoiseConfig(NoiseConfig other)
            {
                octaves = other.octaves;
                persistence = other.persistence;
                frequency = other.frequency;
                amplitude = other.amplitude;
                frequencyMultiplier = other.frequencyMultiplier;
                xOffset = other.xOffset;
                yOffset = other.yOffset;
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

                return octaves == other.octaves &&
                       persistence.EqualsEpsilon(other.persistence) &&
                       frequency.EqualsEpsilon(other.frequency) &&
                       amplitude.EqualsEpsilon(other.amplitude) &&
                       frequencyMultiplier.EqualsEpsilon(other.frequencyMultiplier) &&
                       blockThresholdsTerrain.Count == other.blockThresholdsTerrain.Count &&
                       blockThresholdsSky.Count == other.blockThresholdsSky.Count &&
                       xOffset.EqualsEpsilon(other.xOffset) &&
                       yOffset.EqualsEpsilon(other.yOffset);
            }
        }

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

        public static float OctavePerlin(float x, float y)
        {
            var total = 0.0f;
            var frequency = StaticNoiseConfig.frequency;
            var amplitude = StaticNoiseConfig.amplitude;
            var octaves = StaticNoiseConfig.octaves;
            var maxValue = 0.0f;
            for (var i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise((x + StaticNoiseConfig.xOffset) * frequency, (y + StaticNoiseConfig.yOffset) * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= StaticNoiseConfig.persistence;
                frequency *= StaticNoiseConfig.frequencyMultiplier;
            }

            var noise = total / maxValue;

            return noise;
        }
    }
}