using System;
using System.Collections.Generic;

namespace Utils
{
    [Serializable]
    public class TerrainGenerationModel
    {
        public List<Layer> Layers;

        public TerrainGenerationModel(TerrainGenerationModel other)
        {
            Layers = new List<Layer>();
            for (var i = 0; i < other.Layers.Count; ++i)
            {
                Layers.Add(new Layer(other.Layers[i]));
            }
        }

        public bool Equals(TerrainGenerationModel other)
        {
            if (Layers.Count == other.Layers.Count)
            {
                for (int i = 0; i < Layers.Count; i++)
                {
                    if (!Layers[i].Equals(other.Layers[i]))
                        return false;
                }
            }
            else
                return false;

            return true;
        }
    }

    [Serializable]
    public class Layer
    {
        public List<NoiseConfig> HeightNoises;
        public List<NoiseConfig> InLayerNoises;
        public List<BlockThresholdStruct> Thresholds;
        public int Depth;

        public Layer(Layer other)
        {
            HeightNoises = new List<NoiseConfig>();
            for (var i = 0; i < other.HeightNoises.Count; i++)
            {
                HeightNoises.Add(new NoiseConfig(other.HeightNoises[i]));
            }

            InLayerNoises = new List<NoiseConfig>();
            for (var i = 0; i < other.InLayerNoises.Count; i++)
            {
                InLayerNoises.Add(new NoiseConfig(other.InLayerNoises[i]));
            }

            Thresholds = new List<BlockThresholdStruct>();
            for (var i = 0; i < other.Thresholds.Count; ++i)
            {
                Thresholds.Add(new BlockThresholdStruct(other.Thresholds[i].type, other.Thresholds[i].threshold));
            }

            Depth = other.Depth;
        }

        public bool Equals(Layer other)
        {
            if (HeightNoises.Count == other.HeightNoises.Count)
            {
                for (int i = 0; i < HeightNoises.Count; i++)
                {
                    if (!HeightNoises[i].Equals(other.HeightNoises[i]))
                        return false;
                }
            }
            else
                return false;

            if (InLayerNoises.Count == other.InLayerNoises.Count)
            {
                for (var i = 0; i < InLayerNoises.Count; i++)
                {
                    if (!InLayerNoises[i].Equals(other.InLayerNoises[i]))
                        return false;
                }
            }
            else
                return false;

            if (Thresholds.Count == other.Thresholds.Count)
            {
                for (var i = 0; i < Thresholds.Count; i++)
                {
                    if (!Thresholds[i].Equals(other.Thresholds[i]))
                        return false;
                }
            }
            else
                return false;

            return Depth == other.Depth;
        }
    }

    [Serializable]
    public struct BlockThresholdStruct
    {
        public int type;
        public float threshold;

        public BlockThresholdStruct(int type, float threshold)
        {
            this.type = type;
            this.threshold = threshold;
        }
    }

    [Serializable]
    public struct NoiseConfig
    {
        public FastNoiseLite.NoiseType Type;
        public FastNoiseLite.FractalType FractalType;
        public int Octaves;
        public float Gain;
        public float Frequency;
        public float Lacunarity;
        public float XAmplitude;
        public float YAmplitude;
        public float XOffset;
        public float YOffset;

        public NoiseConfig(NoiseConfig other)
        {
            Type = other.Type;
            FractalType = other.FractalType;
            Octaves = other.Octaves;
            Gain = other.Gain;
            Frequency = other.Frequency;
            Lacunarity = other.Lacunarity;
            XAmplitude = other.XAmplitude;
            YAmplitude = other.YAmplitude;
            XOffset = other.XOffset;
            YOffset = other.YOffset;
        }

        public bool Equals(NoiseConfig other)
        {
            return Type == other.Type &&
               FractalType == other.FractalType &&
               Octaves == other.Octaves &&
               Gain.EqualsEpsilon(other.Gain) &&
               Frequency.EqualsEpsilon(other.Frequency) &&
               Lacunarity.EqualsEpsilon(other.Lacunarity) &&
               XAmplitude.EqualsEpsilon(other.XAmplitude) &&
               YAmplitude.EqualsEpsilon(other.YAmplitude) &&
               XOffset.EqualsEpsilon(other.XOffset) &&
               YOffset.EqualsEpsilon(other.YOffset);
        }
    }

    public class ConfiguredNoise
    {
        private readonly FastNoiseLite _noise;
        private float _xOffset;
        private float _yOffset;

        public ConfiguredNoise()
        {
            _noise = new FastNoiseLite();
        }

        public void Configure(NoiseConfig config)
        {
            _noise.SetNoiseType(config.Type);
            _noise.SetFractalOctaves(config.Octaves);
            _noise.SetFractalGain(config.Gain);
            _noise.SetFrequency(config.Frequency);
            _noise.SetFractalType(config.FractalType);
            _xOffset = config.XOffset;
            _yOffset = config.YOffset;
        }

        public float GetNoise(float x, float y)
        {
            return _noise.GetNoise(x + _xOffset, y + _yOffset);
        }
    }

    public class ConfiguredNoisesForLayer
    {
        public List<ConfiguredNoise> HeightConfiguredNoises;
        public List<ConfiguredNoise> InLayerConfiguredNoises;

        public ConfiguredNoisesForLayer(Layer layer)
        {
            HeightConfiguredNoises = new List<ConfiguredNoise>();
            InLayerConfiguredNoises = new List<ConfiguredNoise>();

            for (var i = 0; i < layer.HeightNoises.Count; i++)
            {
                HeightConfiguredNoises.Add(new ConfiguredNoise());
                HeightConfiguredNoises[i].Configure(layer.HeightNoises[i]);
            }

            for (var i = 0; i < layer.InLayerNoises.Count; i++)
            {
                InLayerConfiguredNoises.Add(new ConfiguredNoise());
                InLayerConfiguredNoises[i].Configure(layer.InLayerNoises[i]);
            }
        }
    }
}
