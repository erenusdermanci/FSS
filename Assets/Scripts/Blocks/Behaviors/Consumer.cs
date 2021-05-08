using System.Collections.Generic;

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
    }
}
