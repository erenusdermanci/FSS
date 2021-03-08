using System.Collections.Generic;
using System.Threading;
using DataComponents;
using MonoBehaviours;
using Utils;
using static BlockConstants;

namespace ChunkTasks
{
    public class GenerationTask : ChunkTask
    {
        public ThreadLocal<System.Random> Rng;
        private System.Random _rng;
        private Dictionary<int, ConfiguredNoisesForLayer> _noisesPerLayer;

        public GenerationTask(Chunk chunk) : base(chunk)
        {
        }

        protected override void Execute()
        {
            if (CancellationToken.IsCancellationRequested)
                CancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < Chunk.BlockCounts.Length; ++i)
                Chunk.BlockCounts[i] = 0;

            Chunk.blockData.colors = new byte[Chunk.Size * Chunk.Size * 4];
            Chunk.blockData.types = new int[Chunk.Size * Chunk.Size];

            if (ProceduralGenerator.IsEnabled)
            {
                _rng = Rng.Value;

                var generationModel = ProceduralGenerator.StaticGenerationModel;
                ConfigureNoises(generationModel);
                GenerateProcedurally(generationModel);
            }
            else
                GenerateEmpty();

            for (var i = 0; i < Chunk.Size * Chunk.Size; i++)
                Chunk.BlockCounts[Chunk.blockData.types[i]] += 1;

            Chunk.Dirty = true; // TODO: Calculate if the chunk is really dirty
        }

        private void ConfigureNoises(TerrainGenerationModel model)
        {
            _noisesPerLayer = new Dictionary<int, ConfiguredNoisesForLayer>();

            for (var i = 0; i < model.Layers.Count; i++)
                _noisesPerLayer.Add(i, new ConfiguredNoisesForLayer(model.Layers[i]));
        }

        private void GenerateProcedurally(TerrainGenerationModel model)
        {
            for (var x = 0; x < Chunk.Size; x++)
            {
                if (CancellationToken.IsCancellationRequested)
                    CancellationToken.ThrowIfCancellationRequested();
                // for each x in our world
                var verticalIdxPerLayer = new int[model.Layers.Count];
                var totalDepth = 0;

                for (int i = 0; i < model.Layers.Count; i++)
                {
                    // for each layer
                    var layer = model.Layers[i];
                    var vertNoiseAcc = 0f;
                    var yAmpTotal = 1f;
                    totalDepth += layer.Depth;

                    for (int j = 0; j < layer.HeightNoises.Count; j++)
                    {
                        // for each noise
                        var xAmp = layer.HeightNoises[j].XAmplitude;
                        var yAmp = layer.HeightNoises[j].YAmplitude;
                        var vertNoise = _noisesPerLayer[i].HeightConfiguredNoises[j].GetNoise((float)((Chunk.Position.x * Chunk.Size) + x) / (Chunk.Size * xAmp), 0);
                        vertNoiseAcc += vertNoise;
                        yAmpTotal *= yAmp;
                    }

                    vertNoiseAcc /= layer.HeightNoises.Count;
                    var vertIdx = (int)((vertNoiseAcc * Chunk.Size * yAmpTotal) - totalDepth);

                    verticalIdxPerLayer[i] = vertIdx;
                }

                for (var y = 0; y < Chunk.Size; y++)
                {
                    var separator = (int)(Chunk.Position.y * Chunk.Size) + y; // what layer does this Y belong to for this X?
                    var layerIdx = GetLayerForY(verticalIdxPerLayer, separator);

                    // Generate y within layer
                    GenerateBlock(model, x, y, layerIdx);
                }
            }
        }

        private void GenerateEmpty()
        {
            for (var i = 0; i < Chunk.Size * Chunk.Size; i++)
            {
                Chunk.blockData.colors[i * 4] = BlockColors[(int) Blocks.Air].r;
                Chunk.blockData.colors[i * 4 + 1] = BlockColors[(int) Blocks.Air].g;
                Chunk.blockData.colors[i * 4 + 2] = BlockColors[(int) Blocks.Air].b;
                Chunk.blockData.colors[i * 4 + 3] = BlockColors[(int) Blocks.Air].a;
                Chunk.blockData.types[i] = (int) Blocks.Air;
            }
        }

        private void GenerateBlock(TerrainGenerationModel model, int x, int y, int layerIdx)
        {
            if (CancellationToken.IsCancellationRequested)
                CancellationToken.ThrowIfCancellationRequested();

            var layer = model.Layers[layerIdx];

            var noiseAcc = 0f;
            for (int i = 0; i < layer.InLayerNoises.Count; i++)
            {
                // for each noise
                var xAmp = layer.InLayerNoises[i].XAmplitude;
                var yAmp = layer.InLayerNoises[i].YAmplitude;

                var noise = _noisesPerLayer[layerIdx].InLayerConfiguredNoises[i].GetNoise(
                    (float)((Chunk.Position.x * Chunk.Size) + x) / (Chunk.Size * xAmp),
                    (float)((Chunk.Position.y * Chunk.Size) + y) / (Chunk.Size * yAmp));

                noiseAcc += noise;
            }

            noiseAcc /= layer.InLayerNoises.Count;

            var block = (int)GetBlockFromNoise(layer, noiseAcc);
            var blockColor = BlockColors[block];

            var idx = y * Chunk.Size + x;

            var shiftAmount = Helpers.GetRandomShiftAmount(_rng, BlockColorMaxShift[block]);
            Chunk.blockData.colors[idx * 4] = Helpers.ShiftColorComponent(blockColor.r, shiftAmount);
            Chunk.blockData.colors[idx * 4 + 1] = Helpers.ShiftColorComponent(blockColor.g, shiftAmount);
            Chunk.blockData.colors[idx * 4 + 2] = Helpers.ShiftColorComponent(blockColor.b, shiftAmount);
            Chunk.blockData.colors[idx * 4 + 3] = blockColor.a;
            Chunk.blockData.types[idx] = block;
        }

        private static Blocks GetBlockFromNoise(Layer layer, float noise)
        {
            var thresholds = layer.Thresholds;

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (noise <= thresholds[i].threshold)
                    return thresholds[i].type;
            }

            return Blocks.Border;
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