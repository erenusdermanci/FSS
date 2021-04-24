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
        public int blockToUse;
        private SpellManager _spellManager;
        private ParticleSystem.Particle[] _particles;
        private readonly List<ParticleCollisionEvent> _events = new List<ParticleCollisionEvent>();
        private ParticleSystem _particleSystem;
        private ParticleSystem.EmitParams _emitParams;

        // Start is called before the first frame update
        private void Awake()
        {
            var blockDescriptor = BlockConstants.BlockDescriptors[blockToUse];
            _particleSystem = GetComponent<ParticleSystem>();
            _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
            _emitParams.startColor = new Color32(
                blockDescriptor.Color.r,
                blockDescriptor.Color.g,
                blockDescriptor.Color.b,
                blockDescriptor.Color.a);

            _spellManager = GetComponentInParent<SpellManager>();
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (!(mainCamera is null))
            {
                Vector2 mousePos = Input.mousePosition;
                if (_spellManager.selectedBlock == blockToUse && Input.GetMouseButton(1))
                {
                    Vector2 pos = mainCamera.WorldToScreenPoint(transform.position);
                    var dir = mousePos - pos;
                    var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    _particleSystem.Emit(_emitParams, 100);
                }

                Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);
                var count = _particleSystem.GetParticles(_particles);

                for (var i = 0; i < count; ++i)
                {
                    var p = _particles[i];
                    Vector2 position = p.position;

                    var targetDirection = (mouseWorldPos - position).normalized;
                    var force = targetDirection * 0.1f;
                    _particles[i].velocity += (Vector3)force;
                }
                _particleSystem.SetParticles(_particles, count);
            }
        }

        private void OnParticleCollision(GameObject other)
        {
            var count = _particleSystem.GetCollisionEvents(other, _events);
            if (count == 0)
                return;

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
                if (serverChunk.GetBlockType(blockYInChunk * Chunk.Size + blockXInChunk) != BlockConstants.Air)
                    continue;
                var blockDescriptor = BlockConstants.BlockDescriptors[blockToUse];
                serverChunk.PutBlock(blockXInChunk, blockYInChunk, blockToUse,
                    blockDescriptor.Color.r,
                    blockDescriptor.Color.g,
                    blockDescriptor.Color.b,
                    blockDescriptor.Color.a,
                    blockDescriptor.InitialStates,
                    blockDescriptor.BaseHealth, 0f, 0);
            }
        }

        private Vector2i GetChunkPosition(float worldX, float worldY)
        {
            return new Vector2i((int) Mathf.Floor(worldX / Chunk.Size),
                (int) Mathf.Floor(worldY / Chunk.Size));
        }
    }
}
