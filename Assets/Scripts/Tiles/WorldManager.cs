using System;
using System.Collections.Generic;
using System.Linq;
using Chunks;
using Chunks.Client;
using Chunks.Server;
using Entities;
using Serialized;
using Tiles.Tasks;
using Tools;
using UnityEngine;
using Utils;
using Utils.UnityHelpers;
using Color = UnityEngine.Color;
using Helpers = Utils.UnityHelpers.Helpers;

namespace Tiles
{
    public class WorldManager : MonoBehaviour
    {
        public int tileGridThickness; // the number of tiles in each direction around the central tile
        private readonly TileMap _serverTileMap = new TileMap();
        private int _maxLoadedTiles;

        private TileTaskScheduler _tileTaskScheduler;

        public ChunkLayer[] ChunkLayers { get; private set; }

        public static int UpdatedFlag;

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        private Vector2i _cameraChunkPosition;
        private bool _cameraHasMoved;
        private Vector2i _oldCameraChunkPosition;
        private Vector2i _currentTilePosition;

        private GameObjectPool[] _chunkPools;

        [NonSerialized]
        public ClientCollisionManager CollisionManager;

        public EntityManager EntityManager { get; private set; }

        private void Awake()
        {
            ChunkLayers = transform.GetComponentsInChildren<ChunkLayer>();

            EntityManager = transform.GetComponentInChildren<EntityManager>();

            _tileTaskScheduler = new TileTaskScheduler();
            _chunkPools = new GameObjectPool[ChunkLayer.TotalChunkLayers];

            _maxLoadedTiles = (2 * tileGridThickness + 1) * (2 * tileGridThickness + 1);
            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                _chunkPools[i] = new GameObjectPool(ChunkLayers[i],
                    Tile.TotalSize * _maxLoadedTiles);
            }

            CollisionManager = new ClientCollisionManager(this);
        }

        private void Start()
        {
            _tileTaskScheduler.GetTaskManager(TileTaskTypes.Load).Processed += OnTileLoaded;
            _tileTaskScheduler.GetTaskManager(TileTaskTypes.Save).Processed += OnTileSaved;

            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
                _oldCameraChunkPosition = _cameraChunkPosition;
                UpdateCameraHasMoved();
            }

            InitializeTileMap();

            GlobalConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;

            UpdatedFlag = 1;
        }

        private void InitializeTileMap()
        {
            var initialTilePos = TileHelpers.GetTilePositionFromChunkPosition(_cameraChunkPosition);
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
            _cameraChunkPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldCameraChunkPosition == _cameraChunkPosition)
                return false;

            _oldCameraChunkPosition = _cameraChunkPosition;
            return true;
        }

        private void HandleTileMapLoading()
        {
            var newTilePosition = TileHelpers.GetTilePositionFromChunkPosition(_cameraChunkPosition);

            if (newTilePosition != _currentTilePosition)
            {
                var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(newTilePosition, tileGridThickness);
                foreach (var tilePosition in newTilePositions)
                {
                    if (_serverTileMap.Contains(tilePosition))
                        continue;

                    _tileTaskScheduler.QueueForLoad(tilePosition);
                }

                _currentTilePosition = newTilePosition;
            }
        }

        private void HandleTileMapUnloading()
        {
            // unload faraway tiles
            foreach (var tile in _serverTileMap.Tiles())
            {
                if (Math.Abs(_currentTilePosition.x - tile.Position.x) >= tileGridThickness + 1
                    || Math.Abs(_currentTilePosition.y - tile.Position.y) >= tileGridThickness + 1)
                {
                    QueueTileForSave(tile);
                }
            }
        }

        private void QueueTileForSave(Tile tile)
        {
            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                foreach (var position in tile.GetChunkPositions())
                {
                    if (ChunkLayers[i].chunkSimulator != null)
                    {
                        var chunk = ChunkLayers[i].ServerChunkMap[position];
                        ChunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, false);
                    }
                }
            }
            var tileSaveTask = _tileTaskScheduler.QueueForSave(tile);
            if (tileSaveTask == null)
                return;

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                var idx = 0;
                foreach (var position in tileSaveTask.Tile.GetChunkPositions())
                {
                    if (ChunkLayers[i].ServerChunkMap.Contains(position))
                    {
                        var chunkToSave = ChunkLayers[i].ServerChunkMap[position];
                        if (tileSaveTask.TileData != null && chunkToSave != null)
                            tileSaveTask.TileData.Value.chunkLayers[i][idx] = chunkToSave.Data;
                    }
                    idx++;
                }
            }

            var entitiesToSave = new List<EntityData>[ChunkLayer.TotalChunkLayers];
            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
                entitiesToSave[i] = new List<EntityData>();
            var entitiesToDestroy = new List<Entity>();
            foreach (var entity in EntityManager.Entities.Values)
            {
                var entityPosition = entity.transform.position;
                if (TileHelpers.GetTilePositionFromWorldPosition(entityPosition) == tileSaveTask.Tile.Position)
                {
                    entitiesToSave[(int) entity.chunkLayerType].Add(new EntityData
                    {
                        x = entityPosition.x,
                        y = entityPosition.y,
                        id = entity.id,
                        chunkLayer = (int) entity.chunkLayerType,
                        dynamic = entity.dynamic,
                        generateCollider = entity.generateCollider,
                        resourceName = entity.ResourceName
                    });
                    entitiesToDestroy.Add(entity);
                }
            }

            foreach (var entity in entitiesToDestroy)
                Destroy(entity.gameObject);

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                if (tileSaveTask.TileData != null)
                    tileSaveTask.TileData.Value.entities[i] = entitiesToSave[i].ToArray();
            }
        }

        private void OnTileSaved(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                foreach (var position in tileTask.Tile.GetChunkPositions())
                {
                    var serverChunk = ChunkLayers[i].ServerChunkMap[position];
                    serverChunk?.Dispose();
                    ChunkLayers[i].ServerChunkMap.Remove(position);

                    var clientChunk = ChunkLayers[i].ClientChunkMap[position];
                    clientChunk?.Dispose();
                    ChunkLayers[i].ClientChunkMap.Remove(position);
                }
            }

            _serverTileMap.Remove(tileTask.Tile.Position);
            tileTask.Tile.Dispose();
        }

        private void OnTileLoaded(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;
            _serverTileMap.Add(tileTask.Tile);

            for (var i = 0; i < ChunkLayer.TotalChunkLayers; ++i)
            {
                var idx = 0;
                foreach (var position in tileTask.Tile.GetChunkPositions())
                {
                    var chunk = new ChunkServer
                    {
                        Position = position
                    };
                    if (tileTask.TileData != null)
                        chunk.Data = tileTask.TileData.Value.chunkLayers[i][idx];
                    else
                    {
                        chunk.Initialize();
                        chunk.GenerateEmpty();
                    }
                    ChunkLayers[i].ServerChunkMap.Add(chunk);
                    if (ChunkLayers[i].chunkSimulator != null)
                        ChunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, true);

                    ChunkLayers[i].ClientChunkMap.Add(CreateClientChunk(_chunkPools[i], chunk));
                    idx++;
                }
            }

            if (tileTask.TileData != null)
            {
                foreach (var entityLayer in tileTask.TileData.Value.entities)
                {
                    foreach (var entityData in entityLayer)
                    {
                        if (!GlobalConfig.StaticGlobalConfig.levelDesignMode && !entityData.dynamic)
                            continue;
                        var entityObject = Instantiate((GameObject) Resources.Load(entityData.resourceName), EntityManager.transform, true);
                        entityObject.transform.position = new Vector2(entityData.x, entityData.y);
                        var entity = entityObject.GetComponent<Entity>();
                        entity.id = entityData.id;
                        entity.dynamic = entityData.dynamic;
                        entity.generateCollider = entityData.generateCollider;
                        entity.SetChunkLayerType((ChunkLayerType) entityData.chunkLayer);
                        EntityManager.Entities.Add(entity.id, entity);
                        if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
                            EntityManager.QueueEntityRemoveFromMap(entity);
                    }
                }
            }
        }

        private static ChunkClient CreateClientChunk(GameObjectPool chunkPool, ChunkServer chunkServer)
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

        public ChunkServer GetChunk(Vector2i position, ChunkLayerType layerType)
        {
            var chunkMap = ChunkLayers[(int) layerType].ServerChunkMap;
            return chunkMap.Contains(position) ? chunkMap[position] : null;
        }

        public void QueueChunkForReload(Vector2i chunkPosition, ChunkLayerType layerType)
        {
            ChunkLayers[(int) layerType].QueueChunkForReload(chunkPosition);
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        // This is not multi-platform compatible, not reliable and not called between scene loads
        private void OnApplicationQuit()
        {
            if (!GlobalConfig.StaticGlobalConfig.deleteSaveOnExit)
            {
                _tileTaskScheduler.CancelLoad();

                if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
                    EntityManager.BlitStaticEntities();

                var tiles = _serverTileMap.Tiles().ToList();
                foreach (var tile in tiles)
                {
                    QueueTileForSave(tile);
                }

                _tileTaskScheduler.ForceSave();
            }
            else
            {
                _tileTaskScheduler.CancelLoad();
                _tileTaskScheduler.ForceSave();

                try
                {
                    Helpers.RemoveFilesInDirectory(TileHelpers.TilesSavePath);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
            }
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
