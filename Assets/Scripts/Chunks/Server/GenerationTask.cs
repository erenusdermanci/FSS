using Chunks.Tasks;
using static Chunks.ChunkLayer;

namespace Chunks.Server
{
    public class GenerationTask : ChunkTask<ChunkServer>
    {
        public GenerationTask(ChunkServer chunk, ChunkLayerType layerType) : base(chunk, layerType)
        {
        }

        protected override void Execute()
        {
            if (ShouldCancel()) return;

            Chunk.Initialize();

            Chunk.GenerateEmpty();

            Chunk.Dirty = true;
        }
    }
}
