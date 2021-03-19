using Blocks;
using Serialized;

namespace Chunks
{
    public class ChunkServer : Chunk
    {
        public BlockData Data;
        public readonly int[] BlockUpdatedFlags = new int[Size * Size];

        public readonly ChunkDirtyRect[] DirtyRects = new ChunkDirtyRect[4];
        public static readonly int[] DirtyRectX = { 0, Size / 2, 0, Size / 2 }; // 2 3
        public static readonly int[] DirtyRectY = { 0, 0, Size / 2, Size / 2 }; // 0 1
        public bool Dirty;

        public ChunkServer()
        {
            for (var i = 0; i < DirtyRects.Length; ++i)
            {
                DirtyRects[i].Reset();
                DirtyRects[i].Initialized = false;
            }
        }

        public void Initialize()
        {
            Data.colors = new byte[Size * Size * 4];
            Data.types = new int[Size * Size];
            Data.stateBitsets = new int[Size * Size];
            Data.healths = new float[Size * Size];
            Data.lifetimes = new float[Size * Size];
        }

        public void PutBlock(int x, int y, int type, int states, float health, float lifetime)
        {
            var i = y * Size + x;
            Data.types[i] = type;
            Data.stateBitsets[i] = states;
            Data.healths[i] = health;
            Data.lifetimes[i] = lifetime;

            UpdateBlockDirty(x, y);

            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (type)
            {
                case BlockConstants.Air:
                case BlockConstants.Cloud:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }

            BlockUpdatedFlags[i] = ChunkManager.UpdatedFlag;
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health, float lifetime)
        {
            var i = y * Size + x;
            Data.colors[i * 4] = r;
            Data.colors[i * 4 + 1] = g;
            Data.colors[i * 4 + 2] = b;
            Data.colors[i * 4 + 3] = a;
            Data.types[i] = type;
            Data.stateBitsets[i] = states;
            Data.healths[i] = health;
            Data.lifetimes[i] = lifetime;

            UpdateBlockDirty(x, y);

            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (type)
            {
                case BlockConstants.Air:
                case BlockConstants.Cloud:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }

            BlockUpdatedFlags[i] = ChunkManager.UpdatedFlag;
        }

        // TODO optimize this, highly critical
        public void UpdateBlockDirty(int x, int y)
        {
            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (Data.types[y * Size + x])
            {
                case BlockConstants.Air:
                case BlockConstants.Cloud:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }

            const int hs = Size / 2;
            // xy
            // 00 -> 0
            // 01 -> 2
            // 10 -> 1
            // 11 -> 3
            var i = (x / hs) | ((y / hs) << 1);

            x -= DirtyRectX[i];
            y -= DirtyRectY[i];
            var d = DirtyRects[i];
            if (DirtyRects[i].X < 0.0f)
            {
                DirtyRects[i].X = x;
                DirtyRects[i].XMax = x;
                DirtyRects[i].Y = y;
                DirtyRects[i].YMax = y;
            }
            else
            {
                if (DirtyRects[i].X > x)
                    DirtyRects[i].X = x;
                if (DirtyRects[i].XMax < x)
                    DirtyRects[i].XMax = x;
                if (DirtyRects[i].Y > y)
                    DirtyRects[i].Y = y;
                if (DirtyRects[i].YMax < y)
                    DirtyRects[i].YMax = y;
            }
            Dirty = true;
        }

        public void GetBlockInfo(int blockIndex, ref BlockInfo blockInfo)
        {
            blockInfo.Type = Data.types[blockIndex];
            blockInfo.StateBitset = Data.stateBitsets[blockIndex];
            blockInfo.Health = Data.healths[blockIndex];
            blockInfo.Lifetime = Data.lifetimes[blockIndex];
        }

        public void SetBlockLifetime(int blockIndex, float lifetime)
        {
            Data.lifetimes[blockIndex] = lifetime;
        }

        public int GetBlockType(int blockIndex)
        {
            return Data.types[blockIndex];
        }

        public void GenerateEmpty()
        {
            var airColor = BlockConstants.BlockDescriptors[BlockConstants.Air].Color;
            for (var i = 0; i < Size * Size; i++)
            {
                Data.colors[i * 4] = airColor.r;
                Data.colors[i * 4 + 1] = airColor.g;
                Data.colors[i * 4 + 2] = airColor.b;
                Data.colors[i * 4 + 3] = airColor.a;
                Data.types[i] = BlockConstants.Air;
                Data.stateBitsets[i] = 0;
                Data.healths[i] = BlockConstants.BlockDescriptors[BlockConstants.Air].BaseHealth;
                Data.lifetimes[i] = 0;
            }
        }

        public struct BlockInfo
        {
            public int Type;
            public int StateBitset;
            public float Health;
            public float Lifetime;

            public bool GetState(int stateToCheck)
            {
                return ((StateBitset >> stateToCheck) & 1) == 1;
            }

            public void SetState(int stateToSet)
            {
                StateBitset |= 1 << stateToSet;
            }

            public void ClearState(int stateToClear)
            {
                StateBitset &= ~(1 << stateToClear);
            }
        }

        public override void Dispose()
        {
        }
    }
}
