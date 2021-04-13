using System;
using Blocks;
using Utils;

namespace Chunks
{
    public abstract class Chunk : IDisposable
    {
        public const int Size = 64;

        public Vector2i Position;

        public bool Dirty;

        public byte[] Colors;
        public Block[] Blocks;

        public abstract void Dispose();
    }
}
