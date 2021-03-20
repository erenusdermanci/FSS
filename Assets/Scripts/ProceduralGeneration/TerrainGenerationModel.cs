using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Utils;

namespace ProceduralGeneration
{
    [Serializable]
    public class TerrainGenerationModel
    {
        [FormerlySerializedAs("Layers")] public List<Layer> layers;

        public TerrainGenerationModel(TerrainGenerationModel other)
        {
            layers = new List<Layer>();
            for (var i = 0; i < other.layers.Count; ++i)
            {
                layers.Add(new Layer(other.layers[i]));
            }
        }

        public bool Equals(TerrainGenerationModel other)
        {
            if (layers.Count == other.layers.Count)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    if (!layers[i].Equals(other.layers[i]))
                        return false;
                }
            }
            else
                return false;

            return true;
        }
    }
}
