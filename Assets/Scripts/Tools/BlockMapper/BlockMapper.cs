using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using Blocks;
using UnityEditor;
using UnityEngine;
using Utils;
using Utils.Drawing;
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

        private Vector2i? _lastPointDrawn;
        private Vector2i? _lastPointDrawnForLine;
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

        private void Update()
        {
            var mainCamera = Camera.main;
            if (mainCamera is null)
                return;
            _mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            DrawBounds();

            var sprite = _spriteRenderer.sprite;
            var blockPosition = new Vector2i(
                (int) (Mathf.Floor(_mouseWorldPosition.x / _texelSize) + sprite.texture.width / 2f),
                (int) (Mathf.Floor(_mouseWorldPosition.y / _texelSize) + sprite.texture.height / 2f)
            );
            if (blockPosition.x < 0 || blockPosition.y < 0 || blockPosition.x >= sprite.texture.width || blockPosition.y >= sprite.texture.height)
                return;
            switch (parameters.tool)
            {
                case DrawingToolType.Brush:
                    UpdateBrush(blockPosition);
                    DrawBrush();
                    break;
                case DrawingToolType.Fill:
                    UpdateFill(blockPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ReloadBlockMaskTexture();
        }

        private void PutBlockType(int blockX, int blockY)
        {
            var sprite = _spriteRenderer.sprite;
            if (blockX < 0 || blockY < 0 || blockX >= sprite.texture.width || blockY >= sprite.texture.height)
                return;
            var blockIndex = blockY * sprite.texture.width + blockX;
            _blockTypes[blockIndex] = parameters.block;
            AssignBlockColor(blockIndex, GetBlockColor(parameters.block));
        }

        private int GetBlockType(int blockX, int blockY)
        {
            var sprite = _spriteRenderer.sprite;
            if (blockX < 0 || blockY < 0 || blockX >= sprite.texture.width || blockY >= sprite.texture.height)
                return UnassignedBlockType - 1;
            return _blockTypes[blockY * _spriteRenderer.sprite.texture.width + blockX];
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

        private void UpdateBrush(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_lastPointDrawnForLine != null && Input.GetKey(KeyCode.LeftShift))
                    Draw.Line(_lastPointDrawnForLine.Value.x, _lastPointDrawnForLine.Value.y,
                        blockPosition.x, blockPosition.y, (x, y) => DrawBlocks(x, y));
            }
            else if (Input.GetMouseButton(0))
            {
                if (_lastPointDrawn == null)
                    DrawBlocks(blockPosition.x, blockPosition.y);
                else
                    Draw.Line(_lastPointDrawn.Value.x, _lastPointDrawn.Value.y, blockPosition.x, blockPosition.y, (x, y) => DrawBlocks(x, y));
                _lastPointDrawn = new Vector2i(blockPosition.x, blockPosition.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _lastPointDrawnForLine = _lastPointDrawn;
                _lastPointDrawn = null;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                if (_lastPointDrawnForLine != null)
                {
                    DrawDebugLine(_lastPointDrawnForLine.Value, blockPosition, UnityEngine.Color.white);
                }
            }
        }

        private void DrawDebugLine(Vector2i start, Vector2i end, Color32 color)
        {
            var sprite = _spriteRenderer.sprite;
            var xStart = (start.x - sprite.texture.width / 2f) * _texelSize + _texelSize / 2f;
            var yStart = (start.y - sprite.texture.height / 2f) * _texelSize + _texelSize / 2f;
            var xEnd = (end.x - sprite.texture.width / 2f) * _texelSize + _texelSize / 2f;
            var yEnd = (end.y - sprite.texture.height / 2f) * _texelSize + _texelSize / 2f;
            Debug.DrawLine(new Vector2(xStart, yStart), new Vector2(xEnd, yEnd), color);
        }

        private void UpdateFill(Vector2i blockPosition)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Draw.Fill(blockPosition.x, blockPosition.y, GetBlockType, PutBlockType);
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

        private void DrawBlocks(int x, int y)
        {
            switch (parameters.brush)
            {
                case DrawingBrushType.Box:
                    Draw.Rectangle(x, y, parameters.size, parameters.size, PutBlockType);
                    break;
                case DrawingBrushType.Circle:
                    Draw.Circle(x, y, parameters.size, PutBlockType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawBrush()
        {
            var sprite = _spriteRenderer.sprite;
            var texelX = Mathf.Floor(_mouseWorldPosition.x * sprite.pixelsPerUnit) / sprite.pixelsPerUnit;
            var texelY = Mathf.Floor(_mouseWorldPosition.y * sprite.pixelsPerUnit) / sprite.pixelsPerUnit;
            switch (parameters.brush)
            {
                case DrawingBrushType.Box:
                {
                    var size = _texelSize * parameters.size;
                    DebugDraw.Square(texelX - size / 2f + _texelSize / 2f,
                        texelY - size / 2f + _texelSize / 2f,
                        size, UnityEngine.Color.red);
                    break;
                }
                case DrawingBrushType.Circle:
                {
                    DebugDraw.Circle(texelX + _texelSize / 2f,
                        texelY + _texelSize / 2f,
                        parameters.size * _texelSize, UnityEngine.Color.red);
                    break;
                }
            }
        }

        private void SaveBlocks()
        {
            using (var file = File.Create(_blocksFilePath, _blockTypes.Length,
                FileOptions.SequentialScan | FileOptions.Asynchronous))
            using (var compressor = new GZipStream(file, CompressionMode.Compress))
            {
                new BinaryFormatter().Serialize(compressor, _blockTypes);
            }
        }

        private void LoadBlocks()
        {
            using (var file = File.Open(_blocksFilePath, FileMode.Open))
            using (var decompressor = new GZipStream(file, CompressionMode.Decompress))
            {
                _blockTypes = (int[]) new BinaryFormatter().Deserialize(decompressor);
            }
        }

        private void OnDestroy()
        {
            SaveBlocks();
        }
    }
}
