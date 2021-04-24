using System.Collections.Generic;
using Blocks;
using Chunks;
using Tiles;
using UnityEngine;
using Utils;

namespace Client
{
    public class ParticleCollisionHandler : MonoBehaviour
    {
        public WorldManager worldManager;
        private readonly List<ParticleCollisionEvent> _events = new List<ParticleCollisionEvent>();
        private ParticleSystem _particleSystem;

        // Start is called before the first frame update
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _particleSystem.Stop();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (_particleSystem.isStopped)
                    _particleSystem.Play();
                else
                    _particleSystem.Stop();
            }
        }

        private void OnParticleCollision(GameObject other)
        {
            var count = _particleSystem.GetCollisionEvents(other, _events);

            for (var i = 0; i < count; ++i)
            {
                var e = _events[i];
                Vector2 worldPosition = e.intersection;
                var blockWorldPosition = new Vector2i((int) Mathf.Floor((worldPosition.x + 0.5f) * Chunk.Size), (int) Mathf.Floor((worldPosition.y + 0.5f) * Chunk.Size));
                var serverChunkPosition = GetChunkPosition(blockWorldPosition.x, blockWorldPosition.y);
                var serverChunk = worldManager.ChunkLayers[(int) ChunkLayerType.Foreground].ServerChunkMap[serverChunkPosition];
                if (serverChunk == null)
                    continue;
                var blockXInChunk = Helpers.Mod(blockWorldPosition.x, Chunk.Size);
                var blockYInChunk = Helpers.Mod(blockWorldPosition.y, Chunk.Size);
                var d = BlockConstants.BlockDescriptors[BlockConstants.Water];
                serverChunk.PutBlock(blockXInChunk, blockYInChunk, BlockConstants.Water, d.Color.r, d.Color.g,
                    d.Color.b, d.Color.a, d.InitialStates, d.BaseHealth, 0f, 0);
            }
        }

        private Vector2i GetChunkPosition(float worldX, float worldY)
        {
            return new Vector2i((int) Mathf.Floor(worldX / Chunk.Size),
                (int) Mathf.Floor(worldY / Chunk.Size));
        }
    }
}
