using DataComponents;
using MonoBehaviours;
using Unity.Jobs;
using UnityEngine;

namespace ChunkTasks
{
    public class GenerationTask : IChunkTask
    {
        public Vector2 ChunkPos;
        public byte[] BlockColors;
        public int[] BlockTypes;

        public void Execute()
        {
            for (var i = 0; i < Chunk.Size * Chunk.Size; ++i)
            {
                var y = i / Chunk.Size;
                var x = i % Chunk.Size;
                var noise = ProceduralGenerator.OctavePerlin(ChunkPos.x + (float) x / Chunk.Size,
                    ChunkPos.y + (float) y / Chunk.Size);
                int block = (int) GetBlockFromNoise(noise);

                var blockColor = Constants.BlockColors[block];
                BlockColors[i * 4] = blockColor.r;
                BlockColors[i * 4 + 1] = blockColor.g;
                BlockColors[i * 4 + 2] = blockColor.b;
                BlockColors[i * 4 + 3] = blockColor.a;
                BlockTypes[i] = block;
            }
        }

        private Constants.Blocks GetBlockFromNoise(float noise)
        {
            var thresholds = ProceduralGenerator.StaticNoiseConfig.blockThresholds;

            for (var i = 0; i < thresholds.Count; ++i)
            {
                if (noise <= thresholds[i].threshold)
                    return thresholds[i].type;
            }

            return Constants.Blocks.Border;
        }
    }
}