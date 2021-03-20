using System;
using UnityEngine.Serialization;
using Utils;

namespace ProceduralGeneration
{
    [Serializable]
    public struct NoiseConfig
    {
        [FormerlySerializedAs("Type")] public FastNoiseLite.NoiseType type;
        [FormerlySerializedAs("FractalType")] public FastNoiseLite.FractalType fractalType;
        [FormerlySerializedAs("Octaves")] public int octaves;
        [FormerlySerializedAs("Gain")] public float gain;
        [FormerlySerializedAs("Frequency")] public float frequency;
        [FormerlySerializedAs("Lacunarity")] public float lacunarity;
        [FormerlySerializedAs("XAmplitude")] public float xAmplitude;
        [FormerlySerializedAs("YAmplitude")] public float yAmplitude;
        [FormerlySerializedAs("XOffset")] public float xOffset;
        [FormerlySerializedAs("YOffset")] public float yOffset;

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
}