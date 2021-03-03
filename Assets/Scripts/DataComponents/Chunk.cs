using System;
using UnityEngine;

namespace DataComponents
{
    public class Chunk : IDisposable
    {
        public static readonly int Size = 64;

        public Texture2D Texture;
        public GameObject GameObject;

        public Vector2 Position;
        public byte[] BlockColors;
        public int[] BlockTypes;
        public byte[] BlockUpdateCooldowns;

        public bool Dirty = false;

        public Chunk(Vector2 position)
        {
            Position = position;
            BlockColors = new byte[Size * Size * 4];
            BlockTypes = new int[Size * Size];
            BlockUpdateCooldowns = new byte[Size * Size];
        }
        
        public void PutBlock(int x, int y, int type)
        {
            var i = y * Size + x;
            BlockColors[i * 4] = Constants.BlockColors[type].r;
            BlockColors[i * 4 + 1] = Constants.BlockColors[type].g;
            BlockColors[i * 4 + 2] = Constants.BlockColors[type].b;
            BlockColors[i * 4 + 3] = Constants.BlockColors[type].a;
            BlockTypes[i] = type;
        }

        public void SetCooldown(int x, int y, byte value)
        {
            BlockUpdateCooldowns[y * Size + x] = value;
            Dirty = true;
        }

        public void Dispose()
        {
            if (GameObject != null)
                GameObject.SetActive(false);
        }
    }
}