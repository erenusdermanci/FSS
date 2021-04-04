using System;
using System.Collections.Generic;
using Chunks;
using Chunks.Server;
using Tools;
using UnityEngine;
using Utils;

namespace Tiles
{
    public class WorldManager : MonoBehaviour
    {
        // Tiles
        private readonly TileMap _serverTileMap = new TileMap();

        public ChunkLayer[] chunkLayers;

        public static int UpdatedFlag;

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        private Vector2i _cameraFlooredPosition;
        private bool _cameraHasMoved;
        private Vector2i _oldCameraFlooredPosition;

        private void Start()
        {
            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
                _oldCameraFlooredPosition = _cameraFlooredPosition;
                UpdateCameraHasMoved();
            }

            UpdateTileMapAroundPosition(GetTileFromPosition(_cameraFlooredPosition));

            GlobalDebugConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;

            UpdatedFlag = 1;
        }

        private void FixedUpdate()
        {
            UpdatedFlag++;

            _cameraHasMoved = UpdateCameraHasMoved();

            if (_cameraHasMoved && _mainCamera != null)
            {
                MainCameraPosition = _mainCamera.transform.position;
            }
        }

        private bool UpdateCameraHasMoved()
        {
            if (_mainCamera == null)
                return false;
            var position = _mainCamera.transform.position;
            _cameraFlooredPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldCameraFlooredPosition == _cameraFlooredPosition)
                return false;

            if (GetTileFromPosition(_oldCameraFlooredPosition) != GetTileFromPosition(_cameraFlooredPosition))
                UpdateTileMapAroundPosition(_cameraFlooredPosition);

            _oldCameraFlooredPosition = _cameraFlooredPosition;
            return true;
        }

        private void UpdateTileMapAroundPosition(Vector2i pos)
        {
            ClearTileMap();
            var tilePositions = GetTilePositionsAroundPosition(pos);
            foreach (var tilePos in tilePositions)
            {
                _serverTileMap.Add(new Tile(tilePos));
            }

            UpdateChunkLayerChunkMaps();
        }

        private void UpdateChunkLayerChunkMaps()
        {
            var chunkMaps = new ChunkMap<ChunkServer>[2];
            var chunkSimulators = new ChunkLayerSimulator[2];
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                // clear existing
                foreach (var clientChunk in chunkLayers[i].ClientChunkMap.Map.Values)
                {
                    clientChunk.Dispose();
                }

                chunkLayers[i].ClientChunkMap.Clear();

                foreach (var serverChunk in chunkLayers[i].ServerChunkMap.Map.Values)
                {
                    serverChunk.Dispose();
                }

                chunkLayers[i].ServerChunkMap.Clear();
                chunkLayers[i]?.chunkSimulator?.Clear();

                chunkMaps[i] = chunkLayers[i].ServerChunkMap;
                chunkSimulators[i] = chunkLayers[i].chunkSimulator;
            }

            // add new tiles
            foreach (var tile in _serverTileMap.Tiles())
            {
                // load the tile
                tile.Load(chunkMaps, chunkSimulators);
            }

            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunkLayers[i].CreateClientChunks();
            }
        }

        private void ClearTileMap()
        {
            // save and unload the tiles before clearing
            var chunkMaps = new ChunkMap<ChunkServer>[2];
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunkMaps[i] = chunkLayers[i].ServerChunkMap;
            }

            foreach (var tile in _serverTileMap.Map.Values)
            {
                tile.Save(chunkMaps);
                tile.Dispose();
            }
            _serverTileMap.Clear();
        }

        private static List<Vector2i> GetTilePositionsAroundPosition(Vector2i flooredPos)
        {
            var positions = new List<Vector2i>();
            var currentTilePos = GetTileFromPosition(flooredPos);

            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    positions.Add(new Vector2i(currentTilePos.x + x, currentTilePos.y + y));
                }
            }
            return positions;
        }

        private static Vector2i GetTileFromPosition(Vector2i pos)
        {
            int x;
            int y;
            if (pos.x < 0)
            {
                x = (int) Mathf.Floor((float) pos.x / Tile.Size);
            }
            else
            {
                x = pos.x / Tile.Size;
            }

            if (pos.y < 0)
            {
                y = (int) Mathf.Floor((float) pos.y / Tile.Size);
            }
            else
            {
                y = pos.y / Tile.Size;
            }

            return new Vector2i(x, y);
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }
    }
}
