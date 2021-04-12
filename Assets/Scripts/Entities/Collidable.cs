using Chunks;
using Tools;
using UnityEngine;
using Utils;

namespace Entities
{
    public abstract class Collidable : MonoBehaviour
    {
        private Vector2 _min;
        private Vector2 _max;

        public readonly Vector2i[] chunkNeighborhoodPositions = new Vector2i[9];
        public readonly bool[] neighborChunkColliderGenerated = new bool[9];

        public Vector2i ChunkPosition;
        private Vector2i? _oldChunkPosition;

        private Collider2D Collider { get; set; }

        protected virtual void Awake()
        {
            Collider = gameObject.GetComponent<Collider2D>();
            if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
            {
                if (Collider != null)
                {
                    Collider.enabled = false;
                    gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            var position = gameObject.transform.position;
            ChunkPosition.Set((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            var moved = _oldChunkPosition == null || _oldChunkPosition.Value != ChunkPosition;

            if (moved)
            {
                ChunkHelpers.GetNeighborhoodChunkPositions(ChunkPosition, chunkNeighborhoodPositions);
                for (var i = 0; i < neighborChunkColliderGenerated.Length; ++i)
                    neighborChunkColliderGenerated[i] = false;
                if (Collider != null)
                {
                    var bounds = Collider.bounds;
                    _min.Set(bounds.min.x - BoundsMultiplier() * bounds.size.x,
                        bounds.min.y - BoundsMultiplier() * bounds.size.y);
                    _max.Set(bounds.max.x + BoundsMultiplier() * bounds.size.x,
                        bounds.max.y + BoundsMultiplier() * bounds.size.y);
                }
            }

            if (_oldChunkPosition == null)
                _oldChunkPosition = new Vector2i(ChunkPosition.x, ChunkPosition.y);
            else
                _oldChunkPosition.Value.Set(ChunkPosition.x, ChunkPosition.y);
        }

        public bool OverlapChunk(Vector2i chunkPosition)
        {
            return _max.x >= chunkPosition.x - 0.5f
                   && chunkPosition.x + 0.5f + 1.0f >= _min.x
                   && _max.y >= chunkPosition.y - 0.5f
                   && chunkPosition.y + 0.5f >= _min.y;
        }

        public float BoundsMultiplier()
        {
            return 0.5f;
        }
    }
}