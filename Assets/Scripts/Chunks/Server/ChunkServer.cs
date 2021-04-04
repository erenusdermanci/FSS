﻿using Blocks;
using Metadata;
using Serialized;

namespace Chunks.Server
{
    public class ChunkServer : Chunk
    {
        public BlockData Data;
        public readonly int[] BlockUpdatedFlags = new int[Size * Size];

        public readonly ChunkDirtyRect[] DirtyRects = new ChunkDirtyRect[4];
        public readonly MetadataManager[] MetadataManagers = new MetadataManager[4];
        public static readonly int[] DirtyRectX = { 0, Size / 2, 0, Size / 2 }; // 2 3
        public static readonly int[] DirtyRectY = { 0, 0, Size / 2, Size / 2 }; // 0 1

        public ChunkServer()
        {
            for (var i = 0; i < DirtyRects.Length; ++i)
            {
                MetadataManagers[i] = new MetadataManager();
                DirtyRects[i].Reset();
                DirtyRects[i].Initialized = false;
            }
        }

        public void Initialize()
        {
            const int totalSize = Size * Size;
            Data.colors = new byte[totalSize * 4];
            Data.types = new int[totalSize];
            Data.stateBitsets = new int[totalSize];
            Data.healths = new float[totalSize];
            Data.lifetimes = new float[totalSize];
            Data.assetIds = new long[totalSize];
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health, float lifetime, long assetId)
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
            Data.assetIds[i] = assetId;

            UpdateBlockDirty(x, y);

            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (type)
            {
                case BlockConstants.Air:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }

            BlockUpdatedFlags[i] = ChunkManager.UpdatedFlag;
        }

        public ref PlantBlockData GetPlantBlockData(int x, int y, int type)
        {
            const int hs = Size / 2;
            var i = (x / hs) | ((y / hs) << 1);
            if (MetadataManagers[i].PlantMetadata == null)
                MetadataManagers[i].PlantMetadata = new PlantBlockData[hs * hs];

            ref var blockData = ref MetadataManagers[i].PlantMetadata[y % hs * hs + x % hs];
            var plantGrower = BlockConstants.BlockDescriptors[type].PlantGrower;
            if (blockData.growthCount < 1 && blockData.depthLevel < plantGrower.MaximumDepthLevels)
                UpdateBlockDirty(x, y);
            if (blockData.id != 0)
                return ref blockData;
            return ref blockData;
        }

        // TODO optimize this, highly critical
        public void UpdateBlockDirty(int x, int y)
        {
            // TODO replace by a lookup in block descriptor to know if this block should update the dirty rect
            switch (Data.types[y * Size + x])
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

        public void SetBlockColor(int x, int y, byte r, byte g, byte b, byte a)
        {
            var blockIndex = y * Size + x;
            Data.colors[blockIndex * 4] = r;
            Data.colors[blockIndex * 4 + 1] = g;
            Data.colors[blockIndex * 4 + 2] = b;
            Data.colors[blockIndex * 4 + 3] = a;
            UpdateBlockDirty(x, y);
        }

        public void GetBlockInfo(int blockIndex, ref Block block)
        {
            block.Type = Data.types[blockIndex];
            block.StateBitset = Data.stateBitsets[blockIndex];
            block.Health = Data.healths[blockIndex];
            block.Lifetime = Data.lifetimes[blockIndex];
            block.AssetId = Data.assetIds[blockIndex];
        }

        public void SetBlockStates(int x, int y, int states)
        {
            Data.stateBitsets[y * Size + x] = states;
            UpdateBlockDirty(x, y);
        }

        public void SetBlockLifetime(int x, int y, float lifetime)
        {
            Data.lifetimes[y * Size + x] = lifetime;
            UpdateBlockDirty(x, y);
        }

        public void SetBlockHealth(int x, int y, float health)
        {
            Data.healths[y * Size + x] = health;
            UpdateBlockDirty(x, y);
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
                Data.assetIds[i] = 0;
            }
        }

        public override void Dispose()
        {
        }
    }
}
