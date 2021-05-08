using System;

namespace Serialized
{
    [Serializable]
    public class TileData
    {
        public BlockData blocks;
        public EntityData[] entities;
    }
}
