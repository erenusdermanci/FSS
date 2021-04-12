using UnityEngine;

namespace Chunks.Client
{
    public class ChunkClient : Chunk
    {
        public Texture2D Texture;
        public GameObject GameObject;
        public PolygonCollider2D Collider = null;
        public byte[] Colors;
        public int[] Types;
        public long[] EntityIds;

        public void UpdateTexture()
        {
            Texture.LoadRawTextureData(Colors);
            Texture.Apply();
        }

        public override void Dispose()
        {
            if (GameObject != null && GameObject.activeSelf && GameObject.activeInHierarchy)
            {
                if (Collider != null)
                    Collider.enabled = false;
                GameObject.SetActive(false);
            }
        }
    }
}
