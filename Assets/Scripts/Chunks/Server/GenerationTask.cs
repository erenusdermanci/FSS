using System;
using System.Collections.Generic;
using Blocks;
using Chunks.Tasks;
using DebugTools;
using ProceduralGeneration;
using Serialized;
using UnityEngine;
using Utils;
using static Chunks.ChunkLayer;
using Random = System.Random;

namespace Chunks.Server
{
    public class GenerationTask : ChunkTask<ChunkServer>
    {
        private Dictionary<int, ConfiguredNoisesForLayer> _noisesPerLayer;

        private Random _rng;

        private const int SmoothTransitionDepthThreshold = 25;

        public GenerationTask(ChunkServer chunk, ChunkLayerType layerType) : base(chunk, layerType)
        {
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            Chunk.Initialize();

            if (GlobalDebugConfig.StaticGlobalConfig.enableProceduralGeneration)
            {
                _rng = StaticRandom.Get();
                var generationModel = ProceduralGenerator.StaticGenerationModel;
                ConfigureNoises(generationModel);
                GenerateProcedurally(generationModel);
            }
            else
                Chunk.GenerateEmpty();

            Chunk.Dirty = true;
        }

        private void ConfigureNoises(TerrainGenerationModel model)
        {
            _noisesPerLayer = new Dictionary<int, ConfiguredNoisesForLayer>();

            for (var i = 0; i < model.layers.Count; i++)
                _noisesPerLayer.Add(i, new ConfiguredNoisesForLayer(model.layers[i]));
        }

        private void GenerateProcedurally(TerrainGenerationModel model)
        {
            for (var x = 0; x < Chunks.Chunk.Size; x++)
            {
                if (ShouldCancel()) return;
                // for each x in our world
                var verticalIdxPerLayer = new int[model.layers.Count];
                var totalDepth = 0;

                for (var i = 0; i < model.layers.Count; i++)
                {
                    // for each layer
                    var layer = model.layers[i];
                    var vertNoiseAcc = 0f;
                    var yAmpTotal = 1f;
                    totalDepth += layer.depth;

                    for (var j = 0; j < layer.heightNoises.Count; j++)
                    {
                        // for each noise
                        var xAmp = layer.heightNoises[j].xAmplitude;
                        var yAmp = layer.heightNoises[j].yAmplitude;
                        var vertNoise = _noisesPerLayer[i].HeightConfiguredNoises[j].GetNoise((Chunk.Position.x * Chunks.Chunk.Size + x) / (Chunks.Chunk.Size * xAmp), 0);
                        vertNoiseAcc += vertNoise;
                        yAmpTotal *= yAmp;
                    }

                    vertNoiseAcc /= layer.heightNoises.Count;
                    var vertIdx = (int)(vertNoiseAcc * Chunks.Chunk.Size * yAmpTotal - totalDepth);

                    verticalIdxPerLayer[i] = vertIdx;
                }

                for (var y = 0; y < Chunks.Chunk.Size; y++)
                {
                    var separator = Chunk.Position.y * Chunks.Chunk.Size + y; // what layer does this Y belong to for this X?


                    // Smooth transition:
                    var layerIdx = GetLayerForY(verticalIdxPerLayer, separator);

                    // Generate y within layer
                    GenerateBlock(model, x, y, layerIdx);
                }
            }
        }

        private void GenerateBlock(TerrainGenerationModel model, int x, int y, int layerIdx)
        {
            if (ShouldCancel()) return;

            var layer = model.layers[layerIdx];

            var noiseAcc = 0f;
            for (var i = 0; i < layer.inLayerNoises.Count; i++)
            {
                // for each noise
                var xAmp = layer.inLayerNoises[i].xAmplitude;
                var yAmp = layer.inLayerNoises[i].yAmplitude;

                var noise = _noisesPerLayer[layerIdx].InLayerConfiguredNoises[i].GetNoise(
                    (Chunk.Position.x * Chunks.Chunk.Size + x) / (Chunks.Chunk.Size * xAmp),
                    (Chunk.Position.y * Chunks.Chunk.Size + y) / (Chunks.Chunk.Size * yAmp));

                noiseAcc += noise;
            }

            noiseAcc /= layer.inLayerNoises.Count;

            var block = GetBlockFromNoise(layer, noiseAcc);
            var blockColor = BlockConstants.BlockDescriptors[block].Color;
            blockColor.Shift(out var r, out var g, out var b);

            var idx = y * Chunks.Chunk.Size + x;

            Chunk.Data.colors[idx * 4] = (byte) (r / ((int)LayerType + 1.0f));
            Chunk.Data.colors[idx * 4 + 1] = (byte) (g / ((int)LayerType + 1.0f));
            Chunk.Data.colors[idx * 4 + 2] = (byte) (b / ((int)LayerType + 1.0f));
            Chunk.Data.colors[idx * 4 + 3] = blockColor.a;
            Chunk.Data.types[idx] = block;
            Chunk.Data.stateBitsets[idx] = 0;
            Chunk.Data.healths[idx] = BlockConstants.BlockDescriptors[block].BaseHealth;
            Chunk.Data.lifetimes[idx] = 0;
        }

        private int GetBlockFromNoise(Layer layer, float noise)
        {
            var thresholds = layer.thresholds;

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (!(noise <= thresholds[i].threshold))
                    continue;
                var block = thresholds[i].type;
                var descriptor = BlockConstants.BlockDescriptors[block];
                switch (LayerType)
                {
                    case ChunkLayerType.Foreground:
                        break;
                    case ChunkLayerType.Background:
                    {
                        if (descriptor.Tag != BlockTags.Solid)
                            block = BlockConstants.Air;
                        break;
                    }
                }
                return block;
            }

            return BlockConstants.Border;
        }

        private int GetLayerForY(int[] vertIdxPerLayer, int y)
        {
            if (y > vertIdxPerLayer[0])
            {
                return GetLayerFromDistance(y,
                    0,
                    0,
                    vertIdxPerLayer[1],
                    0,
                    1);
            }

            if (y < vertIdxPerLayer[vertIdxPerLayer.Length - 1])
                return vertIdxPerLayer.Length - 1;

            var layer = 0;

            while (vertIdxPerLayer[layer] > y)
                layer++;

            try
            {
                if (layer > 0 && layer < vertIdxPerLayer.Length - 1)
                {
                    // Check the distance of this y to the layer it's closest to
                    // And use it to smooth out the transition
                    // ie. have a chance of spawning a block of the closest other layer depending on distance
                    return GetLayerFromDistance(y,
                        layer,
                        vertIdxPerLayer[layer - 1],
                        vertIdxPerLayer[layer],
                        layer - 1,
                        layer + 1);
                }
                else
                {
                    return layer;
                }
            }
            catch (Exception)
            {
                Debug.Log($"layer:{layer}, upper:{layer - 1}, lower:{layer + 1}");
                Debug.Log(vertIdxPerLayer);
                throw;
            }


            return layer;
        }

        private int GetLayerFromDistance(int y,
            int deducedLayer,
            int upperLayerThreshold,
            int lowerLayerThreshold,
            int upperLayer,
            int lowerLayer)
        {
            var upperDistance = upperLayerThreshold - y;
            var lowerDistance = y - lowerLayerThreshold;
            if (deducedLayer != 0 && upperDistance < SmoothTransitionDepthThreshold)
            {
                if ((float)(SmoothTransitionDepthThreshold - upperDistance) / SmoothTransitionDepthThreshold > _rng.NextDouble())
                {
                    // smooth transition
                    return upperLayer;
                }
                else
                {
                    // normal block
                    return deducedLayer;
                }
            }
            else if (lowerDistance < SmoothTransitionDepthThreshold)
            {
                if ((float) (SmoothTransitionDepthThreshold - lowerDistance) / SmoothTransitionDepthThreshold >
                    _rng.NextDouble())
                {
                    return lowerLayer;
                }
                else
                {
                    return deducedLayer;
                }
            }

            return deducedLayer;
        }
    }
}
