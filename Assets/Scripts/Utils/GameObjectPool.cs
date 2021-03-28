using System.Collections.Generic;
using System.Linq;
using Chunks;
using UnityEngine;

namespace Utils
{
    public class GameObjectPool
    {
        private readonly List<GameObject> _pooledObjects;
        private readonly GameObject _objectToPool;
        private readonly ChunkLayer _chunkLayer;

        public GameObjectPool(ChunkLayer chunkLayer, int amountToPool)
        {
            _chunkLayer = chunkLayer;
            _objectToPool = (GameObject) Resources.Load("ChunkObject1");
            _pooledObjects = new List<GameObject>();
            for (var i = 0; i < amountToPool; i++)
            {
                CreateObject();
            }
        }

        public GameObject GetObject()
        {
            foreach (var t in _pooledObjects.Where(t => t != null && !t.activeInHierarchy))
                return t;

            return CreateObject();
        }

        private GameObject CreateObject()
        {
            var obj = Object.Instantiate(_objectToPool, _chunkLayer.transform, true);
            obj.SetActive(false);
            var texture = new Texture2D(Chunk.Size, Chunk.Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(new Vector2(0, 0), new Vector2(Chunk.Size, Chunk.Size)), new Vector2(0.5f, 0.5f),
                Chunk.Size,
                0,
                SpriteMeshType.FullRect);
            obj.layer = _chunkLayer.gameObject.layer;
            spriteRenderer.sortingLayerName = _chunkLayer.type.ToString();
            var collider = obj.AddComponent<PolygonCollider2D>();
            collider.enabled = false;
            _pooledObjects.Add(obj);
            return obj;
        }
    }
}
