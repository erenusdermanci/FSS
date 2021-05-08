using System;

namespace Serialized
{
    [Serializable]
    public struct EntityData
    {
        public float x;
        public float y;
        public long id;
        public bool dynamic;
        public bool generateCollider;
        public string resourceName;
    }
}