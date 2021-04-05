using System;
using System.Collections.Generic;
using System.Linq;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using Tools;
using UnityEngine;
using Utils;
using Utils.UnityHelpers;
using Color = UnityEngine.Color;

namespace Tiles
{
    public class WorldManager : MonoBehaviour
    {
        private const int MaxLoadedTiles = 9;
        private readonly TileMap _serverTileMap = new TileMap();

        public ChunkLayer[] chunkLayers;

        public static int UpdatedFlag;

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        private Vector2i _cameraFlooredPosition;
        private bool _cameraHasMoved;
        private Vector2i _oldCameraFlooredPosition;
        private Vector2i _currentTilePosition;

        private GameObjectPool[] _chunkPools;

        private void Awake()
        {
            _chunkPools = new GameObjectPool[Tile.LayerCount];

            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                _chunkPools[i] = new GameObjectPool(chunkLayers[i],
                    Tile.VerticalSize * Tile.HorizontalSize * MaxLoadedTiles);
            }
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
                _oldCameraFlooredPosition = _cameraFlooredPosition;
                UpdateCameraHasMoved();
            }

            InitializeTileMap();

            GlobalDebugConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;

            UpdatedFlag = 1;
        }

        private void InitializeTileMap()
        {
            var chunkServerMaps = new ChunkMap<ChunkServer>[Tile.LayerCount];
            var chunkClientMaps = new ChunkMap<ChunkClient>[Tile.LayerCount];
            var chunkSimulators = new ChunkLayerSimulator[Tile.LayerCount];
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunkServerMaps[i] = chunkLayers[i].ServerChunkMap;
                chunkClientMaps[i] = chunkLayers[i].ClientChunkMap;
                chunkSimulators[i] = chunkLayers[i].chunkSimulator;
            }

            var initialTilePos = GetTilePositionFromFlooredWorldPosition(_cameraFlooredPosition);
            var newTilePositions = GetTilePositionsAroundCentralTilePosition(initialTilePos);
            foreach (var tilePos in newTilePositions)
            {
                _serverTileMap.Add(new Tile(tilePos));
                _serverTileMap[tilePos]?.Load(chunkServerMaps, chunkClientMaps, chunkSimulators, _chunkPools);
            }
        }

        private void FixedUpdate()
        {
            _mainCamera = Camera.main;

            UpdatedFlag++;

            _cameraHasMoved = UpdateCameraHasMoved();

            if (_cameraHasMoved && _mainCamera != null)
            {
                MainCameraPosition = _mainCamera.transform.position;
                HandleTileMap();
            }

            if (GlobalDebugConfig.StaticGlobalConfig.outlineTiles)
                OutlineTiles();
        }

        private bool UpdateCameraHasMoved()
        {
            if (_mainCamera == null)
                return false;
            var position = _mainCamera.transform.position;
            _cameraFlooredPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldCameraFlooredPosition == _cameraFlooredPosition)
                return false;

            _oldCameraFlooredPosition = _cameraFlooredPosition;
            return true;
        }

        private void HandleTileMap()
        {
            var newTilePos = GetTilePositionFromFlooredWorldPosition(_cameraFlooredPosition);

            if (newTilePos != _currentTilePosition)
            {
                var newTilePositions = GetTilePositionsAroundCentralTilePosition(newTilePos);

                var chunkServerMaps = new ChunkMap<ChunkServer>[Tile.LayerCount];
                var chunkClientMaps = new ChunkMap<ChunkClient>[Tile.LayerCount];
                var chunkSimulators = new ChunkLayerSimulator[Tile.LayerCount];
                for (var i = 0; i < Tile.LayerCount; ++i)
                {
                    chunkServerMaps[i] = chunkLayers[i].ServerChunkMap;
                    chunkClientMaps[i] = chunkLayers[i].ClientChunkMap;
                    chunkSimulators[i] = chunkLayers[i].chunkSimulator;
                }

                foreach (var tilePos in newTilePositions)
                {
                    if (_serverTileMap.Contains(tilePos))
                        continue;

                    _serverTileMap.Add(new Tile(tilePos));
                    _serverTileMap[tilePos]?.Load(chunkServerMaps, chunkClientMaps, chunkSimulators, _chunkPools);
                }

                // clean faraway tiles
                var tiles = _serverTileMap.Tiles().ToList();
                for (var i = 0; i < tiles.Count; ++i)
                {
                    var tilePos = tiles[i].TilePosition;
                    if (Math.Abs(tilePos.x - newTilePos.x) >= 2 || Math.Abs(tilePos.y - newTilePos.y) >= 2)
                    {
                        //faraway tile
                        var tile = _serverTileMap[tilePos];
                        tile?.Save(chunkServerMaps, chunkClientMaps, chunkSimulators);
                        tile?.Dispose();
                        _serverTileMap.Remove(tilePos);
                    }
                }

                _currentTilePosition = newTilePos;
            }
        }

        private static List<Vector2i> GetTilePositionsAroundCentralTilePosition(Vector2i pos)
        {
            var positions = new List<Vector2i>();

            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    positions.Add(new Vector2i(pos.x + x, pos.y + y));
                }
            }
            return positions;
        }

        private static Vector2i GetTilePositionFromFlooredWorldPosition(Vector2i pos)
        {
            int x;
            int y;
            if (pos.x < 0)
            {
                x = (int) Mathf.Floor((float) pos.x / Tile.HorizontalSize);
            }
            else
            {
                x = pos.x / Tile.HorizontalSize;
            }

            if (pos.y < 0)
            {
                y = (int) Mathf.Floor((float) pos.y / Tile.VerticalSize);
            }
            else
            {
                y = pos.y / Tile.VerticalSize;
            }

            return new Vector2i(x, y);
        }

        private void OutlineTiles()
        {
            const float worldOffset = 0.5f;
            var colorsPerIdx = new[] {Color.blue,
                Color.red,
                Color.cyan,
                Color.green,
                Color.magenta,
                Color.yellow,
                Color.black,
                Color.gray,
                Color.white
            };

            var idx = 0;
            foreach (var tile in _serverTileMap.Tiles())
            {
                var tileColor = colorsPerIdx[idx++];

                var x = tile.TilePosition.x * Tile.HorizontalSize;
                var y = tile.TilePosition.y * Tile.VerticalSize;

                // draw the tile borders
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset, y - worldOffset + Tile.VerticalSize), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset + Tile.VerticalSize), new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset + Tile.VerticalSize), new Vector3(x - worldOffset, y - worldOffset + Tile.VerticalSize), tileColor);
            }
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        private void OnDestroy()
        {
            var chunkServerMaps = new ChunkMap<ChunkServer>[Tile.LayerCount];
            var chunkClientMaps = new ChunkMap<ChunkClient>[Tile.LayerCount];
            var chunkSimulators = new ChunkLayerSimulator[Tile.LayerCount];
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                chunkServerMaps[i] = chunkLayers[i].ServerChunkMap;
                chunkClientMaps[i] = chunkLayers[i].ClientChunkMap;
                chunkSimulators[i] = chunkLayers[i].chunkSimulator;
            }

            var tiles = _serverTileMap.Tiles().ToList();
            for (var i = 0; i < tiles.Count; ++i)
            {
                var tile = tiles[i];
                tile.Save(chunkServerMaps, chunkClientMaps, chunkSimulators);
                tile.Dispose();
            }
        }
    }
}
