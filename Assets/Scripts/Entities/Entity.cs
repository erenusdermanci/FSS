using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Blocks;
using Chunks;
using Tools;
using Tools.BlockMapper;
using UnityEditor;
using UnityEngine;
using Utils;
using Utils.Drawing;

namespace Entities
{
    public class Entity : Collidable
    {
        public long id;

        public string ResourceName;

        [HideInInspector]
        public float texelSize;

        [HideInInspector]
        public SpriteRenderer spriteRenderer;

        [HideInInspector]
        public int[] blockTypes;

        [HideInInspector]
        public ChunkLayerType chunkLayerType;
        public bool dynamic;
        public bool generateCollider;
        public bool enableBlockMap;

        private BlockMap _blockMap;
        private string _blocksFilePath;

        private EntityManager _entityManager;

        private Vector2 _oldPosition;

        protected override void Awake()
        {
            base.Awake();

            spriteRenderer = GetComponent<SpriteRenderer>();
            var sprite = spriteRenderer.sprite;
            texelSize = 1.0f / sprite.pixelsPerUnit;

            var assetPath = AssetDatabase.GetAssetPath(sprite);
            _blocksFilePath = assetPath.Substring(0, assetPath.IndexOf('.')) + ".fss";

            if (File.Exists(_blocksFilePath))
            {
                LoadBlocks();
            }
            else
            {
                InitializeBlocks();
            }

            if (transform.parent == null)
            {
                var entityManagerObject = GameObject.Find("EntityManager");
                if (entityManagerObject)
                {
                    transform.parent = entityManagerObject.transform;
                    // assign this entity Unique Id that will be transmitted to blocks when they are put in the grid
                    id = UniqueIdGenerator.Next();
                    _entityManager = entityManagerObject.GetComponent<EntityManager>();
                    _entityManager.Entities.Add(id, this);
                }
            }
            else
            {
                _entityManager = transform.parent.GetComponent<EntityManager>();
            }

            _oldPosition = transform.position;

            if (!GlobalConfig.StaticGlobalConfig.levelDesignMode)
            {
                spriteRenderer.enabled = false;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!GlobalConfig.StaticGlobalConfig.levelDesignMode && dynamic)
            {
                Vector2 newPosition = transform.position;
                if (!_oldPosition.Equals(newPosition))
                {
                    RemoveFromMap(_oldPosition);
                    BlitIntoMap(newPosition);
                }

                _oldPosition = newPosition;
            }

            if (enableBlockMap && _blockMap == null)
                CreateBlockMap();
            else if (_blockMap != null && !enableBlockMap)
            {
                Destroy(_blockMap.gameObject);
                _blockMap = null;
            }
        }

        private void BlitIntoMap(Vector2 position)
        {
            var sprite = spriteRenderer.sprite;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => _entityManager.PutEntityBlock(this, i, j, x, y));
        }

        private void RemoveFromMap(Vector2 position)
        {
            var sprite = spriteRenderer.sprite;
            var w = sprite.texture.width;
            var h = sprite.texture.height;
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - w / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - h / 2;
            Draw.Rectangle(w / 2, h / 2, w, h, (i, j) => _entityManager.RemoveEntityBlock(this, i, j, x, y));
        }

        private void CreateBlockMap()
        {
            var blockMapObject = Instantiate((GameObject) Resources.Load("BlockMap"), transform, true);
            blockMapObject.SetActive(false);
            var sprite = spriteRenderer.sprite;
            var width = sprite.texture.width;
            var height = sprite.texture.height;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var blockMapSpriteRenderer = blockMapObject.GetComponent<SpriteRenderer>();
            blockMapSpriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(new Vector2(0, 0), new Vector2(width, height)), new Vector2(0.5f, 0.5f),
                sprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            blockMapObject.layer = gameObject.layer;
            _blockMap = blockMapObject.GetComponent<BlockMap>();
            _blockMap.entity = this;
            blockMapObject.SetActive(true);
        }

        public void ReloadBlockMap()
        {
            if (enableBlockMap)
                _blockMap.Reload();
        }

        private void InitializeBlocks()
        {
            var sprite = spriteRenderer.sprite;
            blockTypes = new int[sprite.texture.width * sprite.texture.height];
            for (var i = 0; i < blockTypes.Length; ++i)
                blockTypes[i] = BlockConstants.UnassignedBlockType;
        }

        private void SaveBlocks()
        {
            using (var file = File.Create(_blocksFilePath, blockTypes.Length,
                FileOptions.SequentialScan | FileOptions.Asynchronous))
            using (var compressor = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressor, blockTypes);
                compressor.Flush();
            }
        }

        private void LoadBlocks()
        {
            using (var file = File.Open(_blocksFilePath, FileMode.Open))
            using (var decompressor = new GZipStream(file, CompressionMode.Decompress))
            {
                blockTypes = (int[]) new BinaryFormatter().Deserialize(decompressor);
            }
        }

        public void PutBlockType(int blockX, int blockY, int type)
        {
            var sprite = spriteRenderer.sprite;
            if (blockX < 0 || blockY < 0 || blockX >= sprite.texture.width || blockY >= sprite.texture.height)
                return;
            var blockIndex = blockY * sprite.texture.width + blockX;
            GetBlockColor(blockX, blockY, out _, out _, out _, out var a);
            if (a == 0)
                type = BlockConstants.Air;
            blockTypes[blockIndex] = type;
            if (enableBlockMap)
                _blockMap.AssignBlockColor(blockIndex, BlockConstants.GetBlockColor(type));
        }

        public int GetBlockType(int blockX, int blockY)
        {
            var sprite = spriteRenderer.sprite;
            if (blockX < 0 || blockY < 0 || blockX >= sprite.texture.width || blockY >= sprite.texture.height)
                return BlockConstants.UnassignedBlockType - 1;
            return blockTypes[blockY * sprite.texture.width + blockX];
        }

        public void GetBlockColor(int blockX, int blockY, out byte r, out byte g, out byte b, out byte a)
        {
            var sprite = spriteRenderer.sprite;
            var color = sprite.texture.GetPixels32()[blockY * sprite.texture.width + blockX];
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public void SetChunkLayerType(ChunkLayerType layerType)
        {
            if (chunkLayerType == layerType)
                return;
            chunkLayerType = layerType;
            spriteRenderer = spriteRenderer == null ? GetComponent<SpriteRenderer>() : spriteRenderer;
            switch (layerType)
            {
                case ChunkLayerType.Foreground:
                    spriteRenderer.sortingLayerName = "ForegroundEntities";
                    break;
                case ChunkLayerType.Background:
                    spriteRenderer.sortingLayerName = "BackgroundEntities";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDestroy()
        {
            if (enableBlockMap)
                SaveBlocks();
            if (transform.parent != null && _entityManager != null)
            {
                _entityManager.Entities.Remove(id);
            }
        }
    }
}