using System;
using Utils;

namespace Chunks
{
    public abstract class Chunk : IDisposable
    {
        public const int Size = 64;

        public Vector2i Position;

        public bool Dirty;

        public abstract void Dispose();
    }
}
