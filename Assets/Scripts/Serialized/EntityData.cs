using System;

namespace Serialized
{
    [Serializable]
    public struct EntityData
    {
        public float x;
        public float y;
        public long id;
        public int chunkLayer;
        public bool dynamic;
        public string resourceName;
    }
}