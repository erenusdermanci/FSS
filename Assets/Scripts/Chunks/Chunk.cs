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

        public readonly ChunkDirtyRect[] DirtyRects = new ChunkDirtyRect[4];
        public static readonly int[] DirtyRectX = { 0, Size / 2, 0, Size / 2 }; // 2 3
        public static readonly int[] DirtyRectY = { 0, 0, Size / 2, Size / 2 }; // 0 1
        public bool Dirty;

        public Chunk()
        {
            for (var i = 0; i < DirtyRects.Length; ++i)
            {
                DirtyRects[i].Reset();
            }
        }

        public void UpdateTexture()
        {
            Texture.LoadRawTextureData(Data.colors);
            Texture.Apply();
        }

        public void PutBlock(int x, int y, int type, int states, float health, float lifetime)
        {
            var i = y * Size + x;
            Data.types[i] = type;
            Data.stateBitsets[i] = states;
            Data.healths[i] = health;
            Data.lifetimes[i] = lifetime;
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

            UpdateBlockDirty(x, y);
        }

        // TODO optimize this, highly critical
        public void UpdateBlockDirty(int x, int y)
        {
            // TODO replace by a lookup in block descriptor to now if this block should update the dirty rect
            switch (Data.types[y * Size + x])
            {
                case BlockConstants.Air:
                case BlockConstants.Cloud:
                case BlockConstants.Stone:
                case BlockConstants.Metal:
                case BlockConstants.Border:
                    return;
            }
            int i;
            if (x < Size / 2)
                i = y < Size / 2 ? 0 : 2;
            else
                i = y < Size / 2 ? 1 : 3;

            x -= DirtyRectX[i];
            y -= DirtyRectY[i];
            if (DirtyRects[i].X < 0.0f)
            {
                DirtyRects[i].X = x;
                DirtyRects[i].XMax = x;
                DirtyRects[i].Y = y;
                DirtyRects[i].YMax = y;
            }
            else
            {
                if (DirtyRects[i].X > x)
                    DirtyRects[i].X = x;
                if (DirtyRects[i].XMax < x)
                    DirtyRects[i].XMax = x;
                if (DirtyRects[i].Y > y)
                    DirtyRects[i].Y = y;
                if (DirtyRects[i].YMax < y)
                    DirtyRects[i].YMax = y;
            }
            Dirty = true;
        }

        public void Dispose()
        {
            if (GameObject != null && GameObject.activeSelf && GameObject.activeInHierarchy)
            {
                GameObject.SetActive(false);
            }
        }

        public void GetBlockInfo(int blockIndex, ref BlockInfo blockInfo)
        {
            blockInfo.Type = Data.types[blockIndex];
            blockInfo.StateBitset = Data.stateBitsets[blockIndex];
            blockInfo.Health = Data.healths[blockIndex];
            blockInfo.Lifetime = Data.lifetimes[blockIndex];
        }

        public int GetBlockType(int blockIndex)
        {
            return Data.types[blockIndex];
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
