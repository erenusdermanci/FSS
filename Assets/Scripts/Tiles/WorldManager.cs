using System;
using System.Linq;
using Chunks;
using Chunks.Client;
using Chunks.Server;
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
        private const int MaxLoadedTiles = 9;
        private readonly TileMap _serverTileMap = new TileMap();

        private TileTaskScheduler _tileTaskScheduler;

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
            _tileTaskScheduler = new TileTaskScheduler(chunkLayers);

            _chunkPools = new GameObjectPool[Tile.LayerCount];

            for (var i = 0; i < Tile.LayerCount; ++i)
            {
                _chunkPools[i] = new GameObjectPool(chunkLayers[i],
                    Tile.VerticalSize * Tile.HorizontalSize * MaxLoadedTiles);
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
            var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(initialTilePos);
            foreach (var tilePos in newTilePositions)
            {
                _tileTaskScheduler.QueueForLoad(tilePos);
            }

            _tileTaskScheduler.ForceLoad();
        }

        private void Update()
        {
            _tileTaskScheduler.Update();
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

        private void HandleTileMap()
        {
            var newTilePos = TileHelpers.GetTilePositionFromFlooredWorldPosition(_cameraFlooredPosition);

            if (newTilePos != _currentTilePosition)
            {
                var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(newTilePos);

                foreach (var tilePos in newTilePositions)
                {
                    if (_serverTileMap.Contains(tilePos))
                        continue;

                    _tileTaskScheduler.QueueForLoad(tilePos);
                }

                // clean faraway tiles
                var tiles = _serverTileMap.Tiles().ToList();
                for (var i = 0; i < tiles.Count; ++i)
                {
                    var tilePos = tiles[i].Position;
                    if (Math.Abs(tilePos.x - newTilePos.x) >= 2 || Math.Abs(tilePos.y - newTilePos.y) >= 2)
                         _tileTaskScheduler.QueueForSave(tiles[i]);
                }

                _currentTilePosition = newTilePos;
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

                    if (chunkLayers[i].chunkSimulator != null)
                        chunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, false);

                    // remove the chunks from the chunkmap
                    chunk.Dispose();
                    chunkLayers[i].ServerChunkMap.Remove(chunk.Position);

                    chunkLayers[i].ClientChunkMap[chunk.Position]?.Dispose();
                    chunkLayers[i].ClientChunkMap.Remove(chunk.Position);
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

                    if (chunkLayers[i].chunkSimulator != null)
                        chunkLayers[i].chunkSimulator.UpdateSimulationPool(chunk, true);

                    chunkLayers[i].ClientChunkMap.Add(CreateClientChunk(_chunkPools[i], chunk));
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
    }
}
