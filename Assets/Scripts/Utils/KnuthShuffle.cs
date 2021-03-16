using System;

namespace Utils
{
    public class KnuthShuffle
    {
        private readonly int[] _shuffle;

        public KnuthShuffle(int seed, int size)
        {
            _shuffle = new int[size];

            for (var i = 0; i < size; ++i)
            {
                _shuffle[i] = i;
            }

            var rng = new Random(seed);

            while (size > 1)
            {
                var i = rng.Next(size--);
                var tmp = _shuffle[size];
                _shuffle[size] = _shuffle[i];
                _shuffle[i] = tmp;
            }
        }

        public int this[int i] => _shuffle[i];
    }
}