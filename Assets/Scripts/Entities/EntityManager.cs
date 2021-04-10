﻿using System;
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
        private Dictionary<long, Entity> _entities = new Dictionary<long, Entity>();

        private WorldManager _worldManager;

        private void Awake()
        {
            _worldManager = transform.parent.GetComponent<WorldManager>();
        }

        private void BlitEntity(Entity entity)
        {
            var sprite = entity.spriteRenderer.sprite;
            var position = entity.transform.position;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => PutBlock(entity, i, j, x, y));

            // hide the sprite so we see the blocks in the grid
            entity.GetComponent<SpriteRenderer>().enabled = false;
        }

        private void PutBlock(Entity entity, int entityBlockX, int entityBlockY, int entityWorldX, int entityWorldY)
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
                0, BlockConstants.BlockDescriptors[blockType].BaseHealth, 0, 0);
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
            foreach (var entity in _entities.Values)
            {
                if (entity.dynamic)
                    continue;
                BlitEntity(entity);
            }
        }

        public void EntityAwake(Entity entity)
        {
            // assign this entity Unique Id that will be transmitted to blocks when they are put in the grid
            entity.id = UniqueIdGenerator.Next();

            _entities.Add(entity.id, entity);

            // add the block grid snapping script
            var snap = entity.gameObject.AddComponent<EntitySnap>();
            snap.enabled = true;
        }

        private void EntityDestroyed(Entity childEntity)
        {
            _entities.Remove(childEntity.id);
        }
    }
}