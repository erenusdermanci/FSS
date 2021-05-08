using Tiles;
using Tools;
using UnityEngine;
using Utils;

namespace Chunks
{
    public class Chunk
    {
        // any modification needs to also be applied in constants.cginc:chunk_size
        // and repercuted on compute shaders dispatch ThreadGroups and numthreads
        public const int Size = 64;

        public Vector2i Position;

        private readonly WorldManager _worldManager;
        private readonly Tile _tile;

        public Chunk(Tile tile, int x, int y)
        {
            Position.Set(x, y);
            _worldManager = tile.WorldManager;
            _tile = tile;
        }

        public void Update()
        {
            var x = (_tile.Position.x * Tile.HorizontalChunks + Position.x) * Size;
            var y = (_tile.Position.y * Tile.VerticalChunks + Position.y) * Size;
            if (x != 0 || y != 0)
                return;
            var worldWidth = _worldManager.Width * Size;
            var worldHeight = _worldManager.Height * Size;
            _worldManager.swapBehaviorShader.SetInts("position", x, y);
            _worldManager.swapBehaviorShader.SetInts("world_size", worldWidth, worldHeight);
            _worldManager.swapBehaviorShader.SetInt("frame_count", WorldManager.CurrentFrame);
            _worldManager.swapBehaviorShader.SetTexture(_worldManager.SwapBehaviorHandle, "colors", _worldManager.RenderTexture);
            _worldManager.swapBehaviorShader.SetBuffer(_worldManager.SwapBehaviorHandle, "blocks", _worldManager.BlocksBuffer);
            _worldManager.swapBehaviorShader.Dispatch(_worldManager.SwapBehaviorHandle, worldWidth / 8, worldHeight / 8, 1);

            if (GlobalConfig.StaticGlobalConfig.outlineChunks)
                Outline();
        }

        private void Outline()
        {
            const float halfUnit = 0.5f;
            var worldManagerPosition = _worldManager.Position;
            var x = worldManagerPosition.x + _tile.Position.x + Position.x;
            var y = worldManagerPosition.y + _tile.Position.y + Position.y;

            // draw the chunk borders
            var borderColor = UnityEngine.Color.white;
            Debug.DrawLine(new Vector3(x - halfUnit, y - halfUnit), new Vector3(x + halfUnit, y - halfUnit), borderColor);
            Debug.DrawLine(new Vector3(x - halfUnit, y - halfUnit), new Vector3(x - halfUnit, y + halfUnit), borderColor);
            Debug.DrawLine(new Vector3(x + halfUnit, y + halfUnit), new Vector3(x - halfUnit, y + halfUnit), borderColor);
            Debug.DrawLine(new Vector3(x + halfUnit, y + halfUnit), new Vector3(x + halfUnit, y - halfUnit), borderColor);
        }
    }
}
