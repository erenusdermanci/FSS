using System;
using System.Collections.Generic;
using Chunks;
using Chunks.Server;

namespace Blocks.Behaviors
{
    public class Consumer : IBehavior
    {
        private readonly bool[] _isBlockConsumed;
        private readonly int _replaceConsumedBy;
        private readonly int _replacedInto;
        private readonly float _transformProbability;

        public Consumer(IEnumerable<int> consumed,
            int replacedInto,
            int replaceConsumedBy,
            float transformProbability)
        {
            _replacedInto = replacedInto;
            _replaceConsumedBy = replaceConsumedBy;
            _transformProbability = transformProbability;
            _isBlockConsumed = new bool[BlockConstants.NumberOfBlocks];
            for (var i = 0; i < BlockConstants.NumberOfBlocks; ++i)
                _isBlockConsumed[i] = false;
            foreach (var block in consumed)
                _isBlockConsumed[block] = true;
        }

        public unsafe bool Execute(Random rng, ChunkServerNeighborhood chunkNeighborhood, int x, int y, int* directionX, int* directionY, ref bool destroyed)
        {
            var chunks = chunkNeighborhood.GetChunks();
            for (var i = 0; i < 8; ++i)
            {
                var nx = x + directionX[i];
                var ny = y + directionY[i];
                ChunkServerNeighborhood.UpdateOutsideChunk(ref nx, ref ny, out var chunkIndex);
                var chunk = chunks[chunkIndex];
                if (chunk == null)
                    continue;
                ref var neighborBlock = ref chunk.GetBlockInfo(ny * Chunk.Size + nx);
                if (!_isBlockConsumed[neighborBlock.type])
                    continue;
                chunkNeighborhood.ReplaceBlock(x + directionX[i], y + directionY[i], _replaceConsumedBy,
                    BlockConstants.BlockDescriptors[_replaceConsumedBy].InitialStates,
                    BlockConstants.BlockDescriptors[_replaceConsumedBy].BaseHealth, 0, 0);

                if (_transformProbability >= rng.NextDouble())
                {
                    chunkNeighborhood.ReplaceBlock(x, y, _replacedInto,
                        BlockConstants.BlockDescriptors[_replacedInto].InitialStates,
                        BlockConstants.BlockDescriptors[_replacedInto].BaseHealth, 0, 0);
                    destroyed = true;
                }
                return true;
            }

            return true;
        }
    }
}
