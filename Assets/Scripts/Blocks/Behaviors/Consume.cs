using System.Collections.Generic;
using Chunks;

namespace Blocks.Behaviors
{
    public class Consume : IBehavior
    {
        private readonly bool[] _isBlockConsumed;
        private readonly int _transformBlock;

        public Consume(IEnumerable<int> consumedBlocks,
            int transformBlock)
        {
            _transformBlock = transformBlock;
            _isBlockConsumed = new bool[BlockConstants.NumberOfBlocks];
            for (var i = 0; i < BlockConstants.NumberOfBlocks; ++i)
                _isBlockConsumed[i] = false;
            foreach (var block in consumedBlocks)
                _isBlockConsumed[block] = true;
        }

        public unsafe bool Execute(ChunkNeighborhood chunkNeighborhood, int x, int y, int* directionX, int* directionY, ref bool destroyed)
        {
            for (var i = 0; i < 8; ++i)
            {
                ChunkServer.BlockInfo neighborBlock = default;
                var neighborFound = chunkNeighborhood.GetBlockInfo(x + directionX[i], y + directionY[i], ref neighborBlock);
                if (!neighborFound)
                    continue;
                if (_isBlockConsumed[neighborBlock.Type])
                {
                    chunkNeighborhood.ReplaceBlock(x, y, _transformBlock,
                        BlockConstants.BlockDescriptors[_transformBlock].InitialStates,
                        BlockConstants.BlockDescriptors[_transformBlock].BaseHealth, 0);
                    destroyed = true;
                    return true;
                }
            }

            return true;
        }
    }
}