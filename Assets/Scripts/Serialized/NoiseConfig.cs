using System;
using Utils;

namespace Serialized
{
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
}
