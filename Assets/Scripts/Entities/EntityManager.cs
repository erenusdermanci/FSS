using System;
using System.Collections.Generic;
using Blocks;
using Chunks;
using Tiles;
using UnityEngine;
using Utils;
using Utils.Drawing;

namespace Entities
{
    public class EntityManager : MonoBehaviour
    {
        public readonly Dictionary<long, Entity> Entities = new Dictionary<long, Entity>();

        private readonly List<Entity> _entitiesToRemoveFromMap = new List<Entity>();

        private WorldManager _worldManager;

        private void Awake()
        {
            _worldManager = transform.parent.GetComponent<WorldManager>();
        }

        private void FixedUpdate()
        {
            foreach (var entity in _entitiesToRemoveFromMap)
            {
                RemoveEntityFromMap(entity);
            }
            _entitiesToRemoveFromMap.Clear();
        }

        public void QueueEntityRemoveFromMap(Entity entity)
        {
            _entitiesToRemoveFromMap.Add(entity);
        }

        private void BlitEntity(Entity entity)
        {
            var sprite = entity.spriteRenderer.sprite;
            var position = entity.transform.position;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => PutEntityBlock(entity, i, j, x, y));
        }

        public void RemoveEntityFromMap(Entity entity)
        {
            var sprite = entity.spriteRenderer.sprite;
            var position = entity.transform.position;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => RemoveEntityBlock(entity, i, j, x, y));
        }

        public void RemoveEntityBlock(Entity entity, int entityBlockX, int entityBlockY, int entityWorldX,
            int entityWorldY)
        {
            var blockType = entity.GetBlockType(entityBlockX, entityBlockY);
            switch (blockType)
            {
                case BlockConstants.Air:
                    return;
                case BlockConstants.UnassignedBlockType:
                    Debug.Log($"block at x={entityBlockX}, y={entityBlockY} in entity {entity.name} has not been assigned!");
                    return;
            }

            if (blockType < BlockConstants.UnassignedBlockType)
                throw new InvalidOperationException($"position x={entityBlockX}, y={entityBlockY} is invalid within entity {entity.name}");

            var blockWorldX = entityWorldX + entityBlockX;
            var blockWorldY = entityWorldY + entityBlockY;
            var chunkPosition = new Vector2i((int) Mathf.Floor(blockWorldX / (float)Chunk.Size),
                (int) Mathf.Floor(blockWorldY / (float)Chunk.Size));
            var chunk = _worldManager.GetChunk(chunkPosition, entity.chunkLayerType);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod(blockWorldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(blockWorldY, Chunk.Size);
            var blockIndexInChunk = blockYInChunk * Chunk.Size + blockXInChunk;
            if (chunk.GetBlockEntityId(blockIndexInChunk) != entity.id)
                return;

            var descriptor = BlockConstants.BlockDescriptors[BlockConstants.Air];
            var c = descriptor.Color;
            chunk.FastPutBlock(blockXInChunk, blockYInChunk, BlockConstants.Air, c.r, c.g, c.b, c.a,
                0, descriptor.BaseHealth, 0, 0);

            _worldManager.QueueChunkForReload(chunk.Position, entity.chunkLayerType);
        }

        public void PutEntityBlock(Entity entity, int entityBlockX, int entityBlockY, int entityWorldX, int entityWorldY)
        {
            var blockType = entity.GetBlockType(entityBlockX, entityBlockY);
            switch (blockType)
            {
                case BlockConstants.Air:
                    return;
                case BlockConstants.UnassignedBlockType:
                    Debug.Log($"block at x={entityBlockX}, y={entityBlockY} in entity {entity.name} has not been assigned!");
                    return;
            }

            if (blockType < BlockConstants.UnassignedBlockType)
                throw new InvalidOperationException($"position x={entityBlockX}, y={entityBlockY} is invalid within entity {entity.name}");

            entity.GetBlockColor(entityBlockX, entityBlockY, out var r, out var g, out var b, out var a);

            var blockWorldX = entityWorldX + entityBlockX;
            var blockWorldY = entityWorldY + entityBlockY;
            var chunkPosition = new Vector2i((int) Mathf.Floor(blockWorldX / (float)Chunk.Size),
                (int) Mathf.Floor(blockWorldY / (float)Chunk.Size));
            var chunk = _worldManager.GetChunk(chunkPosition, entity.chunkLayerType);
            if (chunk == null)
                return;

            var blockXInChunk = Helpers.Mod(blockWorldX, Chunk.Size);
            var blockYInChunk = Helpers.Mod(blockWorldY, Chunk.Size);

            var descriptor = BlockConstants.BlockDescriptors[blockType];
            chunk.PutBlock(blockXInChunk, blockYInChunk, blockType, r, g, b, a,
                0, BlockConstants.BlockDescriptors[blockType].BaseHealth, 0, entity.id);
            if (descriptor.PlantGrower != null)
            {
                ref var plantBlockData = ref chunk.GetPlantBlockData(blockXInChunk, blockYInChunk, blockType);
                if (plantBlockData.id != 0)
                    plantBlockData.Reset(blockType, UniqueIdGenerator.Next());
            }

            _worldManager.QueueChunkForReload(chunk.Position, entity.chunkLayerType);
        }

        public void BlitStaticEntities()
        {
            foreach (var entity in Entities.Values)
            {
                if (entity.dynamic)
                    continue;
                BlitEntity(entity);
            }
        }
    }
}