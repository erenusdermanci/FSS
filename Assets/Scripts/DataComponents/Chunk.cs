using System;
using UnityEngine;

namespace DataComponents
{
    [Serializable]
    public class Chunk : IDisposable
    {
        public const int Size = 64;

        [NonSerialized]
        public Texture2D Texture;
        [NonSerialized]
        public GameObject GameObject;
        [NonSerialized]
        public Vector2 Position;
        
        public byte[] blockColors;
        public int[] blockTypes;
        
        [NonSerialized]
        public byte[] BlockUpdatedFlags;

        [NonSerialized]
        public bool Dirty = false;

        [NonSerialized]
        public int[] BlockCounts = new int[Enum.GetNames(typeof(Constants.Blocks)).Length];

        public Chunk(Vector2 position)
        {
            this.Position = position;
            blockColors = new byte[Size * Size * 4];
            blockTypes = new int[Size * Size];
            BlockUpdatedFlags = new byte[Size * Size];
        }
        
        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a)
        {
            var i = y * Size + x;
            blockColors[i * 4] = r;
            blockColors[i * 4 + 1] = g;
            blockColors[i * 4 + 2] = b;
            blockColors[i * 4 + 3] = a;
            blockTypes[i] = type;
        }

        public void SetUpdatedFlag(int x, int y)
        {
            BlockUpdatedFlags[y * Size + x] = 1;
            Dirty = true;
        }

        public void Dispose()
        {
            if (GameObject != null && GameObject.activeSelf && GameObject.activeInHierarchy)
                GameObject.SetActive(false);
        }
    }
}