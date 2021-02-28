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
            public List<BlockThresholdStruct> blockThresholds;

            public NoiseConfig(NoiseConfig other)
            {
                octaves = other.octaves;
                persistence = other.persistence;
                frequency = other.frequency;
                amplitude = other.amplitude;
                frequencyMultiplier = other.frequencyMultiplier;
                xOffset = other.xOffset;
                yOffset = other.yOffset;
                blockThresholds = new List<BlockThresholdStruct>();
                for (var i = 0; i < other.blockThresholds.Count; i++)
                {
                    blockThresholds.Add(new BlockThresholdStruct(
                        other.blockThresholds[i].type,
                        other.blockThresholds[i].threshold));
                }
            }

            public bool Equals(NoiseConfig other)
            {
                if (blockThresholds.Count == other.blockThresholds.Count)
                {
                    for (var i = 0; i < blockThresholds.Count; ++i)
                    {
                        if (!blockThresholds[i].threshold.EqualsEpsilon(other.blockThresholds[i].threshold))
                            return false;
                        if (blockThresholds[i].type != other.blockThresholds[i].type)
                            return false;
                    }
                }

                return octaves == other.octaves &&
                       persistence.EqualsEpsilon(other.persistence) &&
                       frequency.EqualsEpsilon(other.frequency) &&
                       amplitude.EqualsEpsilon(other.amplitude) &&
                       frequencyMultiplier.EqualsEpsilon(other.frequencyMultiplier) &&
                       blockThresholds.Count == other.blockThresholds.Count &&
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