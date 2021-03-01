using System;
using Unity.Collections;
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

        public Chunk(Vector2 position)
        {
            Position = position;
            BlockColors = new byte[Size * Size * 4];
            BlockTypes = new int[Size * Size];
            BlockUpdateCooldowns = new byte[Size * Size];
        }

        public void Dispose()
        {
            if (GameObject != null)
            {
                GameObject.SetActive(false);
            }
            UnityEngine.Object.Destroy(Texture);
            UnityEngine.Object.Destroy(GameObject);
        }
    }
}