using Blocks;
using Metadata;
using Serialized;
using Tiles;
using Utils;

namespace Chunks.Server
{
    public class ChunkServer : Chunk
    {
        public readonly int[] BlockUpdatedFlags = new int[Size * Size];

        public readonly ChunkDirtyRect[] DirtyRects = new ChunkDirtyRect[4];
        private readonly MetadataManager[] _metadataManagers = new MetadataManager[4];
        public static readonly int[] DirtyRectX = { 0, Size / 2, 0, Size / 2 }; // 2 3
        public static readonly int[] DirtyRectY = { 0, 0, Size / 2, Size / 2 }; // 0 1

        public ChunkServer()
        {
            ResetDirty();
        }

        public void ResetDirty()
        {
            for (var i = 0; i < DirtyRects.Length; ++i)
            {
                _metadataManagers[i] = new MetadataManager();
                DirtyRects[i].Reset();
                DirtyRects[i].Initialized = false;
            }

            Dirty = true;
        }

        public void Initialize()
        {
            const int totalSize = Size * Size;
            Colors = new byte[totalSize * 4];
            Blocks = new Block[totalSize];
        }

        public void FastPutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health,
            float lifetime, long entityId)
        {
            var i = y * Size + x;
            Colors[i * 4] = r;
            Colors[i * 4 + 1] = g;
            Colors[i * 4 + 2] = b;
            Colors[i * 4 + 3] = a;
            ref var block = ref Blocks[i];
            block.type = type;
            block.states = states;
            block.health = health;
            block.lifetime = lifetime;
            block.entityId = entityId;
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health, float lifetime, long entityId)
        {
            var i = y * Size + x;
            Colors[i * 4] = r;
            Colors[i * 4 + 1] = g;
            Colors[i * 4 + 2] = b;
            Colors[i * 4 + 3] = a;
            ref var block = ref Blocks[i];
            block.type = type;
            block.states = states;
            block.health = health;
            block.lifetime = lifetime;
            block.entityId = entityId;

            UpdateBlockDirty(x, y, type);

            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (type)
            {
                case BlockConstants.Air:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }

            BlockUpdatedFlags[i] = WorldManager.UpdatedFlag;
        }

        public ref PlantBlockData GetPlantBlockData(int x, int y, int type)
        {
            const int hs = Size / 2;
            var i = (x / hs) | ((y / hs) << 1);
            if (_metadataManagers[i].PlantMetadata == null)
                _metadataManagers[i].PlantMetadata = new PlantBlockData[hs * hs];

            ref var blockData = ref _metadataManagers[i].PlantMetadata[y % hs * hs + x % hs];
            var plantGrower = BlockConstants.BlockDescriptors[type].PlantGrower;
            if (blockData.growthCount < 1 && blockData.depthLevel < plantGrower.MaximumDepthLevels)
                UpdateBlockDirty(x, y, type);
            if (blockData.id != 0)
                return ref blockData;
            return ref blockData;
        }

        // TODO optimize this, highly critical
        public void UpdateBlockDirty(int x, int y, int blockType)
        {
            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (blockType)
            {
                case BlockConstants.Air:
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

        public void SetBlockColor(int x, int y, Color c)
        {
            var blockIndex = y * Size + x;
            Colors[blockIndex * 4] = c.r;
            Colors[blockIndex * 4 + 1] = c.g;
            Colors[blockIndex * 4 + 2] = c.b;
            Colors[blockIndex * 4 + 3] = c.a;
        }

        public ref Block GetBlockInfo(int blockIndex)
        {
            return ref Blocks[blockIndex];
        }

        public int GetBlockType(int blockIndex)
        {
            return Blocks[blockIndex].type;
        }

        public long GetBlockEntityId(int blockIndex)
        {
            return Blocks[blockIndex].entityId;
        }

        public void GenerateEmpty()
        {
            var airColor = BlockConstants.BlockDescriptors[BlockConstants.Air].Color;
            for (var i = 0; i < Size * Size; i++)
            {
                Colors[i * 4] = airColor.r;
                Colors[i * 4 + 1] = airColor.g;
                Colors[i * 4 + 2] = airColor.b;
                Colors[i * 4 + 3] = airColor.a;
                ref var block = ref Blocks[i];
                block.type = BlockConstants.Air;
                block.states = 0;
                block.health = BlockConstants.BlockDescriptors[BlockConstants.Air].BaseHealth;
                block.lifetime = 0;
                block.entityId = 0;
            }
        }

        public override void Dispose()
        {
        }
    }
}
