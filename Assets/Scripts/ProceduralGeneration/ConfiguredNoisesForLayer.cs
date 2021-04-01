using System.Collections.Generic;
using Serialized;

namespace ProceduralGeneration
{
    public class ConfiguredNoisesForLayer
    {
        public readonly List<ConfiguredNoise> HeightConfiguredNoises;
        public readonly List<ConfiguredNoise> InLayerConfiguredNoises;

        public ConfiguredNoisesForLayer(Layer layer)
        {
            HeightConfiguredNoises = new List<ConfiguredNoise>();
            InLayerConfiguredNoises = new List<ConfiguredNoise>();

            for (var i = 0; i < layer.heightNoises.Count; i++)
            {
                HeightConfiguredNoises.Add(new ConfiguredNoise());
                HeightConfiguredNoises[i].Configure(layer.heightNoises[i]);
            }

            for (var i = 0; i < layer.inLayerNoises.Count; i++)
            {
                InLayerConfiguredNoises.Add(new ConfiguredNoise());
                InLayerConfiguredNoises[i].Configure(layer.inLayerNoises[i]);
            }
        }
    }
}
