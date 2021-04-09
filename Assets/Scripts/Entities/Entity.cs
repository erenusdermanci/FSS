using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Blocks;
using Chunks;
using Tools.BlockMapper;
using UnityEditor;
using UnityEngine;

namespace Entities
{
    public class Entity : MonoBehaviour
    {
        public long id;

        [HideInInspector]
        public float texelSize;

        [HideInInspector]
        public SpriteRenderer spriteRenderer;

        [HideInInspector]
        public int[] blockTypes;

        public ChunkLayer.ChunkLayerType chunkLayerType;

        public bool generateCollider;
        public bool enableBlockMap;

        private BlockMap _blockMap;
        private string _blocksFilePath;

        private void Awake()
        {
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

            if (gameObject.transform.parent != null)
                gameObject.transform.parent.SendMessage("EntityAwake", this);
        }

        private void FixedUpdate()
        {
            if (enableBlockMap && _blockMap == null)
                CreateBlockMap();
            else if (_blockMap != null && !enableBlockMap)
            {
                Destroy(_blockMap.gameObject);
                _blockMap = null;
            }
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
            blockMapObject.layer = transform.gameObject.layer;
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

        private void EntityAwake(Entity childEntity)
        {
        }

        private void EntityDestroyed(Entity childEntity)
        {
        }

        private void OnDestroy()
        {
            if (enableBlockMap)
                SaveBlocks();
            if (gameObject.transform.parent != null && gameObject.transform.parent.gameObject.activeSelf)
                gameObject.transform.parent.SendMessage("EntityDestroyed", this);
        }
    }
}