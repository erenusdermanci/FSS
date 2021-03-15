namespace Chunks
{
    public struct ChunkDirtyRect
    {
        public int X;
        public int Y;
        public int XMax;
        public int YMax;

        public void Reset()
        {
            X = -1;
            Y = -1;
            XMax = -1;
            YMax = -1;
        }
    }
}