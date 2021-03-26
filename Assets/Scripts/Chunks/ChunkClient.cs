using UnityEngine;

namespace Chunks
{
    public class ChunkClient : Chunk
    {
        public Texture2D Texture;
        public GameObject GameObject;
        public PolygonCollider2D Collider;
        public byte[] Colors;
        public int[] Types;

        public void UpdateTexture()
        {
            Texture.LoadRawTextureData(Colors);
            Texture.Apply();
        }

        public override void Dispose()
        {
            if (GameObject != null && GameObject.activeSelf && GameObject.activeInHierarchy)
            {
                Collider.enabled = false;
                GameObject.SetActive(false);
            }
        }
    }
}
