using System;
using System.Collections.Generic;
using Chunks;

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

        public unsafe bool Execute(Random rng, ChunkNeighborhood chunkNeighborhood, int x, int y, int* directionX, int* directionY, ref bool destroyed)
        {
            for (var i = 0; i < 8; ++i)
            {
                ChunkServer.BlockInfo neighborBlock = default;
                var neighborFound = chunkNeighborhood.GetBlockInfo(x + directionX[i], y + directionY[i], ref neighborBlock);
                if (!neighborFound)
                    continue;
                if (_isBlockConsumed[neighborBlock.Type])
                {
                    chunkNeighborhood.ReplaceBlock(x + directionX[i], y + directionY[i], _replaceConsumedBy,
                        BlockConstants.BlockDescriptors[_replaceConsumedBy].InitialStates,
                        BlockConstants.BlockDescriptors[_replaceConsumedBy].BaseHealth, 0);

                    var replacedInto = _replacedInto;
                    if (_transformProbability >= rng.NextDouble())
                    {
                        chunkNeighborhood.ReplaceBlock(x, y, replacedInto,
                            BlockConstants.BlockDescriptors[replacedInto].InitialStates,
                            BlockConstants.BlockDescriptors[replacedInto].BaseHealth, 0);
                        destroyed = true;
                    }
                    return true;
                }
            }

            return true;
        }
    }
}