using System;
using Blocks;
using UnityEngine;

namespace DataComponents
{
    public class Chunk : IDisposable
    {
        public const int Size = 64;
        public Texture2D Texture;
        public GameObject GameObject;
        public Vector2 Position;

        [Serializable]
        public struct BlockData
        {
            public byte[] colors;
            public int[] types;
            public int[] stateBitsets;
            public int[] healths;
        }

        public BlockData Data;

        public readonly byte[] BlockUpdatedFlags = new byte[Size * Size];
        public readonly int[] BlockCounts = new int[BlockLogic.BlockDescriptors.Length];
        public Rect DirtyRect;
        public bool Dirty;

        public Chunk()
        {
            DirtyRect.x = -1;
            DirtyRect.y = -1;
            DirtyRect.xMax = -1;
            DirtyRect.yMax = -1;
        }

        public void UpdateTexture()
        {
            Texture.LoadRawTextureData(Data.colors);
            Texture.Apply();
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a)
        {
            var i = y * Size + x;
            Data.colors[i * 4] = r;
            Data.colors[i * 4 + 1] = g;
            Data.colors[i * 4 + 2] = b;
            Data.colors[i * 4 + 3] = a;
            Data.types[i] = type;
        }

        public void SetUpdatedFlag(int x, int y)
        {
            BlockUpdatedFlags[y * Size + x] = 1;
            Dirty = true;
        }

        public void Dispose()
        {
            if (GameObject != null && GameObject.activeSelf && GameObject.activeInHierarchy)
            {
                GameObject.SetActive(false);
            }
        }
    }
}