using System.Collections.Generic;
using Blocks;
using Chunks.Tasks;
using DebugTools;
using ProceduralGeneration;

namespace Chunks.Server
{
    public class GenerationTask : ChunkTask<ChunkServer>
    {
        private Dictionary<int, ConfiguredNoisesForLayer> _noisesPerLayer;

        public GenerationTask(ChunkServer chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            Chunk.Initialize();

            if (GlobalDebugConfig.StaticGlobalConfig.enableProceduralGeneration)
            {
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
                    var separator = (int)(Chunk.Position.y * Chunks.Chunk.Size) + y; // what layer does this Y belong to for this X?
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

            Chunk.Data.colors[idx * 4] = r;
            Chunk.Data.colors[idx * 4 + 1] = g;
            Chunk.Data.colors[idx * 4 + 2] = b;
            Chunk.Data.colors[idx * 4 + 3] = blockColor.a;
            Chunk.Data.types[idx] = block;
            Chunk.Data.stateBitsets[idx] = 0;
            Chunk.Data.healths[idx] = BlockConstants.BlockDescriptors[block].BaseHealth;
            Chunk.Data.lifetimes[idx] = 0;
        }

        private static int GetBlockFromNoise(Layer layer, float noise)
        {
            var thresholds = layer.thresholds;

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (noise <= thresholds[i].threshold)
                    return thresholds[i].type;
            }

            return BlockConstants.Border;
        }

        private static int GetLayerForY(int[] vertIdxPerLayer, int y)
        {
            if (y > vertIdxPerLayer[0])
                return 0;
            else if (y < vertIdxPerLayer[vertIdxPerLayer.Length - 1])
                return vertIdxPerLayer.Length - 1;

            var layer = 0;

            while (vertIdxPerLayer[layer] > y)
                layer++;

            return layer;
        }
    }
}
