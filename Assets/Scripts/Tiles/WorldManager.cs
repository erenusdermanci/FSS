using System;
using System.Linq;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using Entities;
using Tiles.Tasks;
using Tools;
using UnityEngine;
using Utils;
using Utils.UnityHelpers;
using Color = UnityEngine.Color;

namespace Tiles
{
    public class WorldManager : MonoBehaviour
    {
        public int tileGridThickness; // the number of tiles in each direction around the central tile
        private readonly TileMap _serverTileMap = new TileMap();
        private int _maxLoadedTiles;

        private TileTaskScheduler _tileTaskScheduler;

        private ChunkLayer[] _chunkLayers;

        public static int UpdatedFlag;

        private UpdatedGameObject _player;

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        private Vector2i _cameraFlooredPosition;
        private bool _cameraHasMoved;
        private Vector2i _oldCameraFlooredPosition;
        private Vector2i _currentTilePosition;

        private GameObjectPool[] _chunkPools;

        [NonSerialized]
        public ClientCollisionManager CollisionManager;

        private EntityManager _entityManager;

        private void Awake()
        {
            _chunkLayers = transform.GetComponentsInChildren<ChunkLayer>();

            _entityManager = transform.GetComponentInChildren<EntityManager>();

            _tileTaskScheduler = new TileTaskScheduler(_chunkLayers);
            _chunkPools = new GameObjectPool[Tile.LayerCount];

            _maxLoadedTiles = (2 * tileGridThickness + 1) * (2 * tileGridThickness + 1);
            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                _chunkPools[i] = new GameObjectPool(_chunkLayers[i],
                    Tile.VerticalSize * Tile.HorizontalSize * _maxLoadedTiles);
            }

            CollisionManager = new ClientCollisionManager(_chunkLayers[(int) ChunkLayer.ChunkLayerType.Foreground]);

            var playerGameObject = GameObject.Find("Player");
            if (playerGameObject)
            {
                _player = new UpdatedGameObject(GameObject.Find("Player"));
                CollisionManager.gameObjectsToUpdate.Add(_player);
            }
        }

        private void Start()
        {
            _tileTaskScheduler.GetTaskManager(TileTaskTypes.Load).Processed += OnTileLoaded;
            _tileTaskScheduler.GetTaskManager(TileTaskTypes.Save).Processed += OnTileSaved;

            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
                _oldCameraFlooredPosition = _cameraFlooredPosition;
                UpdateCameraHasMoved();
            }

            InitializeTileMap();

            GlobalConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;

            UpdatedFlag = 1;
        }

        private void InitializeTileMap()
        {
            var initialTilePos = TileHelpers.GetTilePositionFromFlooredWorldPosition(_cameraFlooredPosition);
            var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(initialTilePos, tileGridThickness);
            foreach (var tilePos in newTilePositions)
            {
                _tileTaskScheduler.QueueForLoad(tilePos);
            }

            _tileTaskScheduler.ForceLoad();
        }

        private void Update()
        {
            _tileTaskScheduler.Update();
            CollisionManager.Update();
        }

        private void FixedUpdate()
        {
            _mainCamera = Camera.main;

            UpdatedFlag++;

            _cameraHasMoved = UpdateCameraHasMoved();

            if (_cameraHasMoved && _mainCamera != null)
            {
                MainCameraPosition = _mainCamera.transform.position;
                HandleTileMapLoading();
            }

            HandleTileMapUnloading();

            if (GlobalConfig.StaticGlobalConfig.outlineTiles)
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

        private void HandleTileMapLoading()
        {
            var newTilePos = TileHelpers.GetTilePositionFromFlooredWorldPosition(_cameraFlooredPosition);

            if (newTilePos != _currentTilePosition)
            {
                var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(newTilePos, tileGridThickness);
                foreach (var tilePos in newTilePositions)
                {
                    if (_serverTileMap.Contains(tilePos))
                        continue;

                    _tileTaskScheduler.QueueForLoad(tilePos);
                }

                _currentTilePosition = newTilePos;
            }
        }

        private void HandleTileMapUnloading()
        {
            // unload faraway tiles
            foreach (var tile in _serverTileMap.Tiles())
            {
                if (Math.Abs(_currentTilePosition.x - tile.Position.x) >= tileGridThickness + 1
                    || Math.Abs(_currentTilePosition.y - tile.Position.y) >= tileGridThickness + 1)
                    _tileTaskScheduler.QueueForSave(tile);
            }
        }

        private void OnTileSaved(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;

            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                for (var idx = 0; idx < tileTask.ChunksForMainThread[i].Count; ++idx)
                {
                    var chunk = tileTask.ChunksForMainThread[i][idx];

                    if (_chunkLayers[i].chunkSimulator != null)
                        _chunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, false);

                    // remove the chunks from the chunkmap
                    chunk.Dispose();
                    _chunkLayers[i].ServerChunkMap.Remove(chunk.Position);

                    _chunkLayers[i].ClientChunkMap[chunk.Position]?.Dispose();
                    _chunkLayers[i].ClientChunkMap.Remove(chunk.Position);
                }
            }

            _serverTileMap.Remove(tileTask.Tile.Position);
            tileTask.Tile.Dispose();
        }

        private void OnTileLoaded(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;
            _serverTileMap.Add(tileTask.Tile);

            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                for (var idx = 0; idx < tileTask.ChunksForMainThread[i].Count; ++idx)
                {
                    var chunk = tileTask.ChunksForMainThread[i][idx];

                    if (_chunkLayers[i].chunkSimulator != null)
                        _chunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, true);

                    _chunkLayers[i].ClientChunkMap.Add(CreateClientChunk(_chunkPools[i], chunk));
                }
            }
        }

        private ChunkClient CreateClientChunk(GameObjectPool chunkPool, ChunkServer chunkServer)
        {
            var chunkGameObject = chunkPool.GetObject();
            chunkGameObject.transform.position = new Vector3(chunkServer.Position.x, chunkServer.Position.y, 0);
            var clientChunk = new ChunkClient
            {
                Position = chunkServer.Position,
                Colors = chunkServer.Data.colors, // pointer on ChunkServer colors,
                Types = chunkServer.Data.types, // pointer on ChunkServer types,
                GameObject = chunkGameObject,
                Collider = chunkGameObject.GetComponent<PolygonCollider2D>(),
                Texture = chunkGameObject.GetComponent<SpriteRenderer>().sprite.texture
            };
            chunkGameObject.SetActive(true);
            clientChunk.UpdateTexture();

            return clientChunk;
        }

        public ChunkServer GetChunk(Vector2i position, ChunkLayer.ChunkLayerType layerType)
        {
            var chunkMap = _chunkLayers[(int) layerType].ServerChunkMap;
            return chunkMap.Contains(position) ? chunkMap[position] : null;
        }

        public void QueueChunkForReload(Vector2i chunkPosition, ChunkLayer.ChunkLayerType layerType)
        {
            _chunkLayers[(int) layerType].QueueChunkForReload(chunkPosition);
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        // This is not multi-platform compatible, not reliable and not called between scene loads
        private void OnApplicationQuit()
        {
            _tileTaskScheduler.CancelLoad();

            var tiles = _serverTileMap.Tiles().ToList();
            for (var i = 0; i < tiles.Count; ++i)
            {
                var tile = tiles[i];
                _tileTaskScheduler.QueueForSave(tile);
            }

            _tileTaskScheduler.ForceSave();
        }

        #region Debug

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
                var tileColor = colorsPerIdx[idx];
                idx = (idx + 1) % 9;

                var x = tile.Position.x * Tile.HorizontalSize;
                var y = tile.Position.y * Tile.VerticalSize;

                // draw the tile borders
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset, y - worldOffset + Tile.VerticalSize), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset + Tile.VerticalSize), new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset), tileColor);
                Debug.DrawLine(new Vector3(x - worldOffset + Tile.HorizontalSize, y - worldOffset + Tile.VerticalSize), new Vector3(x - worldOffset, y - worldOffset + Tile.VerticalSize), tileColor);
            }
        }

        #endregion
    }
}
