using System;
using UnityEngine;
using Utils;

namespace DataComponents
{
    public class Chunk : IDisposable
    {
        private static readonly ObjectPool ChunkPool = new ObjectPool();
        
        public const int Size = 64;
        public Texture2D Texture;
        public GameObject GameObject;
        public Vector2 Position;

        [Serializable]
        public struct BlockData
        {
            public byte[] colors;
            public int[] types;
        }

        public BlockData blockData;
        
        public byte[] BlockUpdatedFlags = new byte[Size * Size];
        public bool Dirty = false;
        public int[] BlockCounts = new int[Enum.GetNames(typeof(Constants.Blocks)).Length];

        public Chunk(BlockData data) // loaded from disk
        {
            blockData = data;

            Dirty = true;
        }
        
        public Chunk() // generated
        {
            blockData.colors = new byte[Size * Size * 4];
            blockData.types = new int[Size * Size];
        }

        public void InitializeGameObject(GameObject parent)
        {
            GameObject = ChunkPool.GetObject();
            Texture = GameObject.GetComponent<SpriteRenderer>().sprite.texture;
            GameObject.transform.position = new Vector3(Position.x, Position.y, 0);
            GameObject.transform.parent = parent.transform;
            GameObject.SetActive(true);
        }

        public void UpdateTexture()
        {
            Texture.LoadRawTextureData(blockData.colors);
            Texture.Apply();
        }
        
        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a)
        {
            var i = y * Size + x;
            blockData.colors[i * 4] = r;
            blockData.colors[i * 4 + 1] = g;
            blockData.colors[i * 4 + 2] = b;
            blockData.colors[i * 4 + 3] = a;
            blockData.types[i] = type;
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
            
            this.Save();
        }
    }
}