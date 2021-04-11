﻿using System;

namespace Serialized
{
    [Serializable]
    public struct TileData
    {
        public BlockData[][] chunkLayers;
        public EntityData[][] entities;
    }
}
