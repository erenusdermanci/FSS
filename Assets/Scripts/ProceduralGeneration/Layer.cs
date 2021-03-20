using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace ProceduralGeneration
{
    [Serializable]
    public class Layer
    {
        [FormerlySerializedAs("HeightNoises")] public List<NoiseConfig> heightNoises;
        [FormerlySerializedAs("InLayerNoises")] public List<NoiseConfig> inLayerNoises;
        [FormerlySerializedAs("Thresholds")] public List<BlockThreshold> thresholds;
        [FormerlySerializedAs("Depth")] public int depth;

        public Layer(Layer other)
        {
            heightNoises = new List<NoiseConfig>();
            for (var i = 0; i < other.heightNoises.Count; i++)
            {
                heightNoises.Add(new NoiseConfig(other.heightNoises[i]));
            }

            inLayerNoises = new List<NoiseConfig>();
            for (var i = 0; i < other.inLayerNoises.Count; i++)
            {
                inLayerNoises.Add(new NoiseConfig(other.inLayerNoises[i]));
            }

            thresholds = new List<BlockThreshold>();
            for (var i = 0; i < other.thresholds.Count; ++i)
            {
                thresholds.Add(new BlockThreshold(other.thresholds[i].type, other.thresholds[i].threshold));
            }

            depth = other.depth;
        }

        public bool Equals(Layer other)
        {
            if (heightNoises.Count == other.heightNoises.Count)
            {
                for (int i = 0; i < heightNoises.Count; i++)
                {
                    if (!heightNoises[i].Equals(other.heightNoises[i]))
                        return false;
                }
            }
            else
                return false;

            if (inLayerNoises.Count == other.inLayerNoises.Count)
            {
                for (var i = 0; i < inLayerNoises.Count; i++)
                {
                    if (!inLayerNoises[i].Equals(other.inLayerNoises[i]))
                        return false;
                }
            }
            else
                return false;

            if (thresholds.Count == other.thresholds.Count)
            {
                for (var i = 0; i < thresholds.Count; i++)
                {
                    if (!thresholds[i].Equals(other.thresholds[i]))
                        return false;
                }
            }
            else
                return false;

            return depth == other.depth;
        }
    }
}