using System;
using UnityEngine;
using Utils;

namespace Chunks.Client
{
    [Serializable]
    public class UpdatedGameObject
    {
        public GameObject GameObject;
        public Collider2D GameObjectCollider;
        public Vector2i GameObjectChunkPosition = new Vector2i(0, 0);
        public Vector2i? GameObjectOldChunkPosition;
        public readonly float GameObjectBoundsSizeMultiplier = 0.5f;

        public UpdatedGameObject(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public bool UpdateGameObjectChunkPosition()
        {
            var position = GameObject.transform.position;
            GameObjectChunkPosition.Set((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (GameObjectOldChunkPosition == null)
                GameObjectOldChunkPosition = new Vector2i(0, 0);
            else if (GameObjectOldChunkPosition.Value == GameObjectChunkPosition)
                return false;
            GameObjectOldChunkPosition.Value.Set(GameObjectChunkPosition.x, GameObjectChunkPosition.y);
            return true;
        }
    }
}