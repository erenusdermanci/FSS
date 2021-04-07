using Blocks;
using Entities;
using UnityEngine;
using Color = Utils.Color;

namespace Tools.BlockMapper
{
    public class BlockMap : MonoBehaviour
    {
        [HideInInspector]
        public Entity entity;

        private SpriteRenderer _blockMaskSpriteRenderer;
        private Texture2D _blockMaskTexture;
        private byte[] _blockMaskColors;

        public void Start()
        {
            var sprite = entity.spriteRenderer.sprite;
            _blockMaskSpriteRenderer = GetComponent<SpriteRenderer>();
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
                AssignBlockColor(i, BlockConstants.GetBlockColor(entity.blockTypes[i]));
            Reload();
        }

        public void Reload()
        {
            _blockMaskTexture.LoadRawTextureData(_blockMaskColors);
            _blockMaskTexture.Apply();
        }

        public void AssignBlockColor(int index, Color color)
        {
            _blockMaskColors[index * 4 + 0] = color.r;
            _blockMaskColors[index * 4 + 1] = color.g;
            _blockMaskColors[index * 4 + 2] = color.b;
            _blockMaskColors[index * 4 + 3] = (byte)(color.a / 3f);
        }
    }
}