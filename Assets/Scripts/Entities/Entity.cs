using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public string resourceName;

        [HideInInspector]
        public float texelSize;

        [HideInInspector]
        public SpriteRenderer spriteRenderer;

        private Color32[] _textureColors;

        public Block[] blocks;

        private int _width;
        private int _height;

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
            _width = sprite.texture.width;
            _height = sprite.texture.height;

            _textureColors = sprite.texture.GetPixels32();
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

            if (!GlobalConfig.StaticGlobalConfig.levelDesignMode && !dynamic)
            {
                spriteRenderer.enabled = false;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
            {
                if (enableBlockMap && _blockMap == null)
                    CreateBlockMap();
                else if (_blockMap != null && !enableBlockMap)
                {
                    Destroy(_blockMap.gameObject);
                    _blockMap = null;
                }
            }
            else if (dynamic)
            {
                Vector2 newPosition = transform.position;
                if (!_oldPosition.Equals(newPosition))
                {
                }

                _oldPosition = newPosition;
            }
        }

        private void CreateBlockMap()
        {
            var blockMapObject = Instantiate((GameObject) Resources.Load("BlockMap"), transform, true);
            blockMapObject.SetActive(false);
            var sprite = spriteRenderer.sprite;
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var blockMapSpriteRenderer = blockMapObject.GetComponent<SpriteRenderer>();
            blockMapSpriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(new Vector2(0, 0), new Vector2(_width, _height)), new Vector2(0.5f, 0.5f),
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
            blocks = new Block[_width * _height];
            for (var i = 0; i < blocks.Length; ++i)
            {
                blocks[i] = new Block();
                blocks[i].Initialize(BlockConstants.UnassignedBlockType);
            }
        }

        private void SaveBlocks()
        {
            using (var file = File.Create(_blocksFilePath, blocks.Length,
                FileOptions.SequentialScan | FileOptions.Asynchronous))
            using (var compressor = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressor, blocks.Select(b => b.type).ToArray());
                compressor.Flush();
            }
        }

        private void LoadBlocks()
        {
            int[] blockTypes;
            using (var file = File.Open(_blocksFilePath, FileMode.Open))
            using (var decompressor = new GZipStream(file, CompressionMode.Decompress))
            {
                blockTypes = (int[]) new BinaryFormatter().Deserialize(decompressor);
                decompressor.Flush();
            }

            blocks = blockTypes.Select(bt =>
            {
                var block = new Block();
                block.Initialize(bt);
                return block;
            }).ToArray();
        }

        public void PutBlockType(int blockX, int blockY, int type)
        {
            if (blockX < 0 || blockY < 0 || blockX >= _width || blockY >= _height)
                return;
            var blockIndex = blockY * _width + blockX;
            GetBlockColor(blockX, blockY, out _, out _, out _, out var a);
            if (a == 0)
                type = BlockConstants.Air;
            blocks[blockIndex].type = type;
            if (enableBlockMap)
                _blockMap.AssignBlockColor(blockIndex, BlockConstants.GetBlockColor(type));
        }

        public int GetBlockType(int blockX, int blockY)
        {
            if (blockX < 0 || blockY < 0 || blockX >= _width || blockY >= _height)
                return BlockConstants.UnassignedBlockType - 1;
            return blocks[blockY * _width + blockX].type;
        }

        public void GetBlockColor(int blockX, int blockY, out byte r, out byte g, out byte b, out byte a)
        {
            var sprite = spriteRenderer.sprite;
            var color = _textureColors[blockY * sprite.texture.width + blockX];
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

        private void BlitIntoMap(Vector2 position)
        {
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - _width / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - _height / 2;
            Draw.Rectangle(_width / 2, _height / 2, _width, _height, (i, j) => _entityManager.PutEntityBlock(this, i, j, x, y));
        }

        private void RemoveFromMap(Vector2 position)
        {
            var x = (int) Mathf.Floor((position.x + 0.5f) * Chunk.Size) - _width / 2;
            var y = (int) Mathf.Floor((position.y + 0.5f) * Chunk.Size) - _height / 2;
            Draw.Rectangle(_width / 2, _height / 2, _width, _height, (i, j) => _entityManager.RemoveEntityBlock(this, i, j, x, y));
        }
    }
}
