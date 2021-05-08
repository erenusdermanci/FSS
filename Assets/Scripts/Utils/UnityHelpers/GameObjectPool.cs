using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils.UnityHelpers
{
    public class GameObjectPool
    {
        private readonly List<GameObject> _pooledObjects;
        private readonly GameObject _objectToPool;
        private readonly GameObject _parent;

        public GameObjectPool(GameObject parent, int amountToPool)
        {
            _parent = parent;
            _objectToPool = (GameObject) Resources.Load("Chunk");
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
            var obj = Object.Instantiate(_objectToPool, _parent.transform, true);
            obj.SetActive(false);
            obj.layer = _parent.gameObject.layer;
            _pooledObjects.Add(obj);
            return obj;
        }
    }
}
