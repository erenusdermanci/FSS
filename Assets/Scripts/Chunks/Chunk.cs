using System;
using Blocks;
using Serialized;
using UnityEngine;

namespace Chunks
{
    public class Chunk : IDisposable
    {
        public const int Size = 64;
        public Texture2D Texture;
        public GameObject GameObject;
        public Vector2 Position;

        public BlockData Data;

        public readonly byte[] BlockUpdatedFlags = new byte[Size * Size];
        public readonly int[] BlockCounts = new int[BlockConstants.BlockDescriptors.Length];
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

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health, float lifetime)
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
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states, float health)
        {
            var i = y * Size + x;
            Data.colors[i * 4] = r;
            Data.colors[i * 4 + 1] = g;
            Data.colors[i * 4 + 2] = b;
            Data.colors[i * 4 + 3] = a;
            Data.types[i] = type;
            Data.stateBitsets[i] = states;
            Data.healths[i] = health;
        }

        public void PutBlock(int x, int y, int type, byte r, byte g, byte b, byte a, int states)
        {
            var i = y * Size + x;
            Data.colors[i * 4] = r;
            Data.colors[i * 4 + 1] = g;
            Data.colors[i * 4 + 2] = b;
            Data.colors[i * 4 + 3] = a;
            Data.types[i] = type;
            Data.stateBitsets[i] = states;
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

        public int GetBlockType(int x, int y)
        {
            return Data.types[y * Size + x];
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

        public void FillBlockInfo(int blockIndex, ref BlockInfo blockInfo)
        {
            blockInfo.Type = Data.types[blockIndex];
            blockInfo.StateBitset = Data.stateBitsets[blockIndex];
            blockInfo.Health = Data.healths[blockIndex];
            blockInfo.Lifetime = Data.lifetimes[blockIndex];
        }

        public struct BlockInfo
        {
            public int Type;
            public int StateBitset;
            public float Health;
            public float Lifetime;

            public bool GetState(int stateToCheck)
            {
                return ((StateBitset >> stateToCheck) & 1) == 1;
            }

            public void SetState(int stateToSet)
            {
                StateBitset |= 1 << stateToSet;
            }

            public void ClearState(int stateToClear)
            {
                StateBitset &= ~(1 << stateToClear);
            }
        }
    }
}
