using System.Collections.Generic;
using System.Linq;
using DataComponents;
using UnityEngine;

namespace Utils
{
    public class ObjectPool
    {
        private readonly bool shouldExpand = true;
        private List<GameObject> pooledObjects;
        private GameObject objectToPool;
        private int amountToPool = 100;

        public ObjectPool()
        {
            objectToPool = (GameObject) Resources.Load("ChunkObject1");
            pooledObjects = new List<GameObject>();
            for (var i = 0; i < amountToPool; i++)
            {
                CreateObject();
            }
        }

        public GameObject GetObject()
        {
            foreach (var t in pooledObjects.Where(t => !t.activeInHierarchy))
                return t;

            return !shouldExpand ? null : CreateObject();
        }

        private GameObject CreateObject()
        {
            var obj = Object.Instantiate(objectToPool);
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
            pooledObjects.Add(obj);
            return obj;
        }
    }
}