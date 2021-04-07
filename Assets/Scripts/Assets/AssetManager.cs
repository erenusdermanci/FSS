using System;
using System.Collections.Generic;
using Blocks;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using UnityEngine;
using Utils;
using Utils.Drawing;

namespace Assets
{
    public class AssetManager : MonoBehaviour
    {
        private List<Asset> _assets;

        public ClientCollisionManager clientCollisionManager;
        public ChunkLayer[] chunkLayers;
        private HashSet<Vector2i>[] _chunksLayersToReload;

        private void Awake()
        {
            _chunksLayersToReload = new HashSet<Vector2i>[chunkLayers.Length];
            for (var i = 0; i < chunkLayers.Length; ++i)
                _chunksLayersToReload[i] = new HashSet<Vector2i>();
        }

        private void Update()
        {
            for (var layerIndex = 0; layerIndex < _chunksLayersToReload.Length; ++layerIndex)
            {
                foreach (var chunkPosition in _chunksLayersToReload[layerIndex])
                {
                    var serverChunk = chunkLayers[layerIndex].ServerChunkMap[chunkPosition];
                    if (serverChunk != null)
                    {
                        var chunkDirtyRects = serverChunk.DirtyRects;
                        for (var i = 0; i < chunkDirtyRects.Length; ++i)
                        {
                            chunkDirtyRects[i].Reset();
                            chunkDirtyRects[i].Initialized = false;
                        }

                        serverChunk.Dirty = true;
                    }

                    var clientChunk = chunkLayers[layerIndex].ClientChunkMap[chunkPosition];
                    if (clientChunk != null)
                    {
                        if (layerIndex == (int)ChunkLayer.ChunkLayerType.Foreground)
                            clientCollisionManager.QueueChunkCollisionGeneration(clientChunk);
                        clientChunk.UpdateTexture();
                    }
                }
                _chunksLayersToReload[layerIndex].Clear();
            }
        }

        public void BlitAsset(Asset asset)
        {
            var sprite = asset.spriteRenderer.sprite;
            var position = asset.transform.position;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => PutBlock(asset, i, j, x, y));

            // hide the sprite so we see the blocks in the grid
            asset.GetComponent<SpriteRenderer>().enabled = false;
        }

        private void PutBlock(Asset asset, int assetBlockX, int assetBlockY, int assetWorldX, int assetWorldY)
        {
            var blockType = asset.GetBlockType(assetBlockX, assetBlockY);
            if (blockType == BlockConstants.UnassignedBlockType)
            {
                Debug.Log($"block at x={assetBlockX}, y={assetBlockY} in asset {asset.name} has not been assigned!");
                return;
            }
            if (blockType < BlockConstants.UnassignedBlockType)
                throw new InvalidOperationException($"position x={assetBlockX}, y={assetBlockY} is invalid within asset {asset.name}");

            asset.GetBlockColor(assetBlockX, assetBlockY, out var r, out var g, out var b, out var a);

            var blockWorldX = assetWorldX + assetBlockX;
            var blockWorldY = assetWorldY + assetBlockY;
            var chunk = GetChunkFromWorld(blockWorldX, blockWorldY, asset.chunkLayerType);
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

            _chunksLayersToReload[(int)asset.chunkLayerType].Add(chunk.Position);
        }

        private ChunkServer GetChunkFromWorld(float worldX, float worldY, ChunkLayer.ChunkLayerType layerType)
        {
            var chunkPosition = new Vector2i((int) Mathf.Floor(worldX / Chunk.Size),
                (int) Mathf.Floor(worldY / Chunk.Size));
            return chunkLayers[(int) layerType].ServerChunkMap.Contains(chunkPosition)
                ? chunkLayers[(int) layerType].ServerChunkMap[chunkPosition]
                : null;
        }

        public void AssetAwake(Asset asset)
        {
            _assets.Add(asset);
        }
    }
}