using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Blocks;
using UnityEditor;
using UnityEngine;
using Utils;
using Color = Utils.Color;

namespace Tools.BlockMapper
{
    public class BlockMapper : MonoBehaviour
    {
        private const int UnassignedBlockType = -1;
        private static readonly Color UnassignedBlockColor = new Color(255, 0, 0, 255);

        [HideInInspector]
        public DrawingParameters.DrawingParameters parameters;

        private SpriteRenderer _blockMaskSpriteRenderer;
        private Texture2D _blockMaskTexture;
        private byte[] _blockMaskColors;

        private SpriteRenderer _spriteRenderer;
        private string _blocksFilePath;
        private int[] _blockTypes;
        private float _texelSize;

        private Vector2 _mouseWorldPosition;

        private void Awake()
        {
            parameters = GetComponent<DrawingParameters.DrawingParameters>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            var sprite = _spriteRenderer.sprite;
            _texelSize = 1.0f / sprite.pixelsPerUnit;
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

            InitializeBlockMaskSprite();
        }

        private void InitializeBlockMaskSprite()
        {
            var sprite = _spriteRenderer.sprite;
            _blockMaskSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
            _blockMaskTexture = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            _blockMaskSpriteRenderer.sprite = Sprite.Create(
                _blockMaskTexture,
                new Rect(new Vector2(0, 0),
                    new Vector2(sprite.texture.width, sprite.texture.height)),
                new Vector2(0.5f, 0.5f),
                sprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            _blockMaskColors = new byte[sprite.texture.width * sprite.texture.height * 4];
            for (var i = 0; i < _blockMaskColors.Length / 4; i++)
                AssignBlockColor(i, GetBlockColor(_blockTypes[i]));
            ReloadBlockMaskTexture();
        }

        private void ReloadBlockMaskTexture()
        {
            _blockMaskTexture.LoadRawTextureData(_blockMaskColors);
            _blockMaskTexture.Apply();
        }

        private static Color GetBlockColor(int type)
        {
            return type == UnassignedBlockType ? UnassignedBlockColor : BlockConstants.BlockDescriptors[type].Color;
        }

        private void AssignBlockColor(int index, Color color)
        {
            _blockMaskColors[index * 4 + 0] = color.r;
            _blockMaskColors[index * 4 + 1] = color.g;
            _blockMaskColors[index * 4 + 2] = color.b;
            _blockMaskColors[index * 4 + 3] = (byte)(color.a / 3f);
        }

        private void Update()
        {
            var mainCamera = Camera.main;
            if (mainCamera is null)
                return;
            _mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            UpdateBlockMapping();

            DrawBounds();
            DrawBrush();
        }

        private void UpdateBlockMapping()
        {
            if (Input.GetMouseButton(0))
            {
                var sprite = _spriteRenderer.sprite;
                var blockX = (int) (Mathf.Floor(_mouseWorldPosition.x / _texelSize) + sprite.texture.width / 2f);
                var blockY = (int) (Mathf.Floor(_mouseWorldPosition.y / _texelSize) + sprite.texture.height / 2f);
                if (blockX < 0 || blockY < 0 || blockX >= sprite.texture.width || blockY >= sprite.texture.height)
                    return;
                var blockIndex = blockY * sprite.texture.width + blockX;
                _blockTypes[blockIndex] = parameters.block;
                AssignBlockColor(blockIndex, GetBlockColor(parameters.block));
                ReloadBlockMaskTexture();
            }
        }

        private void InitializeBlocks()
        {
            var sprite = _spriteRenderer.sprite;
            _blockTypes = new int[sprite.texture.width * sprite.texture.height];
            for (var i = 0; i < _blockTypes.Length; ++i)
                _blockTypes[i] = UnassignedBlockType;
        }

        private void DrawBounds()
        {
            var sprite = _spriteRenderer.sprite;
            var textureWorldWidth = sprite.texture.width * _texelSize;
            var textureWorldHeight = sprite.texture.height * _texelSize;
            var x = -textureWorldWidth / 2.0f;
            var y = -textureWorldHeight / 2.0f;
            Debug.DrawLine(new Vector2(x, y), new Vector2(x + textureWorldWidth, y));
            Debug.DrawLine(new Vector2(x, y + textureWorldHeight), new Vector2(x, y));
            Debug.DrawLine(new Vector2(x + textureWorldWidth, y), new Vector2(x + textureWorldWidth, y + textureWorldHeight));
            Debug.DrawLine(new Vector2(x, y + textureWorldHeight), new Vector2(x + textureWorldWidth, y + textureWorldHeight));
        }

        private void DrawBrush()
        {
            var sprite = _spriteRenderer.sprite;
            var texelX = Mathf.Floor(_mouseWorldPosition.x * sprite.pixelsPerUnit) / sprite.pixelsPerUnit;
            var texelY = Mathf.Floor(_mouseWorldPosition.y * sprite.pixelsPerUnit) / sprite.pixelsPerUnit;
            DebugDraw.Rectangle(texelX, texelY, _texelSize, _texelSize, UnityEngine.Color.red);
        }

        private void SaveBlocks()
        {
            using (var file = File.Create(_blocksFilePath, _blockTypes.Length,
                FileOptions.SequentialScan | FileOptions.Asynchronous))
            {
                new BinaryFormatter().Serialize(file, _blockTypes);
            }
        }

        private void LoadBlocks()
        {
            using (var file = File.Open(_blocksFilePath, FileMode.Open))
            {
                var loadedData = new BinaryFormatter().Deserialize(file);
                _blockTypes = (int[]) loadedData;
            }
        }

        private void OnDestroy()
        {
            SaveBlocks();
        }
    }
}
