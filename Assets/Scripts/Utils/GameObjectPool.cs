using System.Collections.Generic;
using System.Linq;
using DataComponents;
using UnityEngine;

namespace Utils
{
    public class GameObjectPool
    {
        private readonly List<GameObject> _pooledObjects;
        private readonly GameObject _objectToPool;

        public GameObjectPool(int amountToPool)
        {
            _objectToPool = (GameObject) Resources.Load("ChunkObject1");
            _pooledObjects = new List<GameObject>();
            for (var i = 0; i < amountToPool; i++)
            {
                CreateObject();
            }
        }

        public GameObject GetObject()
        {
            foreach (var t in _pooledObjects.Where(t => !t.activeInHierarchy))
                return t;

            return CreateObject();
        }

        private GameObject CreateObject()
        {
            var obj = Object.Instantiate(_objectToPool);
            obj.SetActive(false);
            var texture = new Texture2D(Chunk.Size, Chunk.Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            obj.GetComponent<SpriteRenderer>().sprite = Sprite.Create(
                texture,
                new Rect(new Vector2(0, 0), new Vector2(Chunk.Size, Chunk.Size)), new Vector2(0.5f, 0.5f),
                Chunk.Size, 
                0,
                SpriteMeshType.FullRect);
            _pooledObjects.Add(obj);
            return obj;
        }
    }
}