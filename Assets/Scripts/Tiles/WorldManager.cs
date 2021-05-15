using System;
using System.Linq;
using Blocks;
using Chunks;
using Entities;
using Tiles.Tasks;
using Tools;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;
using Helpers = Utils.UnityHelpers.Helpers;

namespace Tiles
{
    public class WorldManager : MonoBehaviour
    {
        public int tileGridThickness; // the number of tiles in each direction around the central tile

        [NonSerialized] public Vector2i Position;
        [NonSerialized] public int Width;
        [NonSerialized] public int Height;

        private readonly TileMap _tileMap = new TileMap();
        private int _maxLoadedTiles;

        private TileTaskScheduler _tileTaskScheduler;

        public ComputeShader swapBehaviorShader;
        public ComputeShader drawRectShader;
        public ComputeShader initColorsShader;
        public ComputeShader applyColorsShader;

        public int SwapBehaviorHandle { get; private set;  }
        public int DrawRectHandle { get; private set;  }
        public int InitColorsHandle { get; private set;  }
        public int ApplyColorsHandle { get; private set; }

        public ComputeBuffer BlocksBuffer { get; private set; }
        public ComputeBuffer IndicesBuffer { get; private set; }
        private readonly RenderTexture[] _textures = new RenderTexture[2];
        private int _textureIndex;
        private MeshRenderer _meshRenderer;

        public static int CurrentFrame;

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        private Vector2i _cameraChunkPosition;
        private bool _cameraHasMoved;
        private Vector2i _oldCameraChunkPosition;
        private Vector2i _currentTilePosition;

        public ComputeBuffer TileBlocksBuffer { get; private set; }
        public Texture2D TileColors { get; private set; }

        public EntityManager EntityManager;

        private bool _spacePressed;

        private unsafe void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            InitColorsHandle = initColorsShader.FindKernel("init_colors");
            DrawRectHandle = drawRectShader.FindKernel("draw_rect");
            SwapBehaviorHandle = swapBehaviorShader.FindKernel("swap_behavior");
            ApplyColorsHandle = applyColorsShader.FindKernel("apply_colors");

            var worldTileSize = tileGridThickness * 2 + 1;
            Width = worldTileSize * Tile.HorizontalChunks;
            Height = worldTileSize * Tile.VerticalChunks;

            /*
             * SEE GPU BLOCK
             ** int lock;
             ** int type;
             ** int states;
             ** float lifetime;
             ** float4 color;
             */
            var blocksStride = sizeof(int) * 2 + sizeof(float) * 7;
            var indexesStride = sizeof(int) * 2;
            var blockCount = worldTileSize * worldTileSize * Tile.ChunkAmount * Chunk.Size * Chunk.Size;
            var lockIndexInit = new lockIndex[blockCount];
            for (var i = 0; i < blockCount; ++i)
            {
                lockIndexInit[i].lockValue = 0;
                lockIndexInit[i].index = i;
            }
            BlocksBuffer = new ComputeBuffer(blockCount, blocksStride);
            IndicesBuffer = new ComputeBuffer(blockCount, indexesStride);
            IndicesBuffer.SetData(lockIndexInit);
            for (var i = 0; i < _textures.Length; ++i)
            {
                _textures[i] = new RenderTexture(worldTileSize * Tile.HorizontalChunks * Chunk.Size,
                    worldTileSize * Tile.VerticalChunks * Chunk.Size, 0)
                {
                    enableRandomWrite = true,
                    autoGenerateMips = false,
                    useMipMap = false,
                    filterMode = FilterMode.Point
                };
                _textures[i].Create();
            }

            _textureIndex = 0;
            _meshRenderer.material.mainTexture = _textures[1];
            var xOffset = -0.5f - Width / 2.0f;
            var yOffset = -0.5f - Height / 2.0f;
            var vertices = new[]
            {
                new Vector3(xOffset, yOffset + Height),
                new Vector3(xOffset + Width, yOffset + Height),
                new Vector3(xOffset, yOffset),
                new Vector3(xOffset + Width, yOffset)
            };
            var mesh = gameObject.GetComponent<MeshFilter>().mesh;
            mesh.vertices = vertices;

            _tileTaskScheduler = new TileTaskScheduler();

            var tileWidth = Tile.HorizontalChunks * Chunk.Size * Chunk.Size;
            var tileHeight = Tile.VerticalChunks * Chunk.Size * Chunk.Size;
            TileBlocksBuffer = new ComputeBuffer(tileWidth * tileHeight, sizeof(Block));
            TileColors = new Texture2D(tileWidth, tileHeight, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };

            var unityPosition = transform.position;
            Position.Set(
                (int) (unityPosition.x + xOffset),
                (int) (unityPosition.y + yOffset)
            );
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            _tileTaskScheduler.GetTaskManager(TileTaskTypes.Load).Processed += OnTileLoaded;
            // _tileTaskScheduler.GetTaskManager(TileTaskTypes.Save).Processed += OnTileSaved;

            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
                _oldCameraChunkPosition = _cameraChunkPosition;
                UpdateCameraHasMoved();
            }

            InitializeTileMap();

            CurrentFrame = 1;
        }

        private void InitializeTileMap()
        {
            foreach (var tilePos in TileHelpers.GetRelativeTilePositions(tileGridThickness))
            {
                _tileTaskScheduler.QueueForLoad(this, new Vector2i(tilePos.x, tilePos.y));
            }

            _tileTaskScheduler.ForceLoad();
        }

        private void Update()
        {
            _tileTaskScheduler.Update();
            if (Input.GetKeyDown(KeyCode.Space))
                _spacePressed = true;
            if (!GlobalConfig.StaticGlobalConfig.stepByStep
                || GlobalConfig.StaticGlobalConfig.stepByStep && _spacePressed)
            {
                _spacePressed = false;
                foreach (var tile in _tileMap.Tiles())
                {
                    tile.Update();
                }
                // GL.Flush();
                // render the next texture while we draw in the current
                _textureIndex = (_textureIndex + 1) % _textures.Length;
                _meshRenderer.material.mainTexture = _textures[(_textureIndex + 1) % _textures.Length];
                ApplyColors();
            }
        }

        private void ApplyColors()
        {
            var worldWidth = Width * Chunk.Size;
            var worldHeight = Height * Chunk.Size;
            applyColorsShader.SetInts("texture_size", worldWidth, worldHeight);
            applyColorsShader.SetTexture(ApplyColorsHandle, "colors", _textures[_textureIndex]);
            applyColorsShader.SetBuffer(ApplyColorsHandle, "blocks", BlocksBuffer);
            applyColorsShader.SetBuffer(ApplyColorsHandle, "indices", IndicesBuffer);
            applyColorsShader.Dispatch(ApplyColorsHandle, worldWidth / 8, worldHeight / 8, 1);
        }

        private void FixedUpdate()
        {
            _mainCamera = Camera.main;

            CurrentFrame++;

            _cameraHasMoved = UpdateCameraHasMoved();

            var unityPosition = transform.position;
            Position.Set(
                (int) (unityPosition.x + -0.5f - Width / 2.0f),
                (int) (unityPosition.y + -0.5f - Height / 2.0f)
            );

            if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
                return;
            if (GlobalConfig.StaticGlobalConfig.pauseSimulation)
                return;

            if (_cameraHasMoved && _mainCamera != null)
            {
                MainCameraPosition = _mainCamera.transform.position;
                // HandleTileMapLoading();
            }

            // HandleTileMapUnloading();

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

        // private void HandleTileMapLoading()
        // {
        //     var newTilePosition = TileHelpers.GetTilePositionFromChunkPosition(_cameraChunkPosition);
        //
        //     if (newTilePosition != _currentTilePosition)
        //     {
        //         var newTilePositions = TileHelpers.GetTilePositionsAroundCentralTilePosition(newTilePosition, tileGridThickness);
        //         foreach (var tilePosition in newTilePositions)
        //         {
        //             if (_serverTileMap.Contains(tilePosition))
        //                 continue;
        //
        //             _tileTaskScheduler.QueueForLoad(tilePosition);
        //         }
        //
        //         _currentTilePosition = newTilePosition;
        //     }
        // }

        // private void HandleTileMapUnloading()
        // {
        //     // unload faraway tiles
        //     foreach (var tile in _serverTileMap.Tiles())
        //     {
        //         if (Math.Abs(_currentTilePosition.x - tile.Position.x) >= tileGridThickness + 1
        //             || Math.Abs(_currentTilePosition.y - tile.Position.y) >= tileGridThickness + 1)
        //         {
        //             QueueTileForSave(tile);
        //         }
        //     }
        // }

        // private void QueueTileForSave(Tile tile)
        // {
        //     var tileSaveTask = _tileTaskScheduler.QueueForSave(tile);
        //     if (tileSaveTask == null)
        //         return;
        //
        //     var idx = 0;
        //     foreach (var position in tileSaveTask.Tile.GetChunkPositions())
        //     {
        //         if (ChunkManager.ChunkMap.Contains(position))
        //         {
        //             var chunkToSave = ChunkManager.ChunkMap[position];
        //             if (tileSaveTask.TileData != null && chunkToSave != null)
        //             {
        //                 ref var blockData = ref tileSaveTask.TileData.blocks;
        //                 var blocks = tileSaveTask.Tile.GetBlocks();
        //                 blockData.colors = tileSaveTask.Tile.GetColorsAsBytes();
        //                 blockData.types = new int[blocks.Length];
        //                 blockData.states = new int[blocks.Length];
        //                 blockData.lifetimes = new float[blocks.Length];
        //                 for (var n = 0; n < blocks.Length; ++n)
        //                 {
        //                     blockData.types[n] = blocks[n].Type;
        //                     blockData.states[n] = blocks[n].States;
        //                     blockData.lifetimes[n] = blocks[n].Lifetime;
        //                 }
        //             }
        //
        //             ChunkManager.ChunkMap.Remove(position);
        //         }
        //         idx++;
        //     }
        //
        //     var entitiesToSave = new List<EntityData>();
        //     var entitiesToDestroy = new List<Entity>();
        //     foreach (var entity in EntityManager.Entities.Values)
        //     {
        //         var entityPosition = entity.transform.position;
        //         if (TileHelpers.GetTilePositionFromWorldPosition(entityPosition) == tileSaveTask.Tile.Position)
        //         {
        //             entitiesToSave.Add(new EntityData
        //             {
        //                 x = entityPosition.x,
        //                 y = entityPosition.y,
        //                 id = entity.id,
        //                 dynamic = entity.dynamic,
        //                 generateCollider = entity.generateCollider,
        //                 resourceName = entity.resourceName
        //             });
        //             entitiesToDestroy.Add(entity);
        //         }
        //     }
        //
        //     foreach (var entity in entitiesToDestroy)
        //         Destroy(entity.gameObject);
        //
        //     if (tileSaveTask.TileData != null)
        //         tileSaveTask.TileData.entities = entitiesToSave.ToArray();
        // }

        private void OnTileSaved(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;

            _tileMap.Remove(tileTask.Tile.Position);
        }

        private void OnTileLoaded(object sender, EventArgs e)
        {
            var tileTask = ((TileTaskEvent) e).Task;
            _tileMap.Add(tileTask.Tile);

            // var idx = 0;
            // foreach (var position in tileTask.Tile.GetChunkPositions())
            // {
            //     var chunk = ChunkManager.CreateChunk(position);
            //     if (tileTask.TileData != null)
            //     {
            //         var blockData = tileTask.TileData.chunks[idx];
            //         var blocks = new Block[blockData.types.Length];
            //         for (var n = 0; n < blockData.types.Length; ++n)
            //         {
            //             blocks[n].Type = blockData.types[n];
            //             blocks[n].States = blockData.states[n];
            //             blocks[n].Lifetime = blockData.lifetimes[n];
            //         }
            //     }
            //     else
            //     {
            //         // TODO: generate empty tile
            //     }
            //     idx++;
            // }

            const int emptyBlock = BlockConstants.Air;
            var descriptor = BlockConstants.BlockDescriptors[emptyBlock];
            DrawRect(
                tileTask.Tile.Position.x * Tile.HorizontalChunks * Chunk.Size,
                tileTask.Tile.Position.y * Tile.VerticalChunks * Chunk.Size,
                Tile.HorizontalChunks * Chunk.Size,
                Tile.VerticalChunks * Chunk.Size,
                emptyBlock,
                descriptor.Color,
                descriptor.InitialStates,
                0
            );

            if (tileTask.TileData != null)
            {
                foreach (var entityData in tileTask.TileData.entities)
                {
                    if (!GlobalConfig.StaticGlobalConfig.levelDesignMode && !entityData.dynamic)
                        continue;
                    var entityObject = Instantiate((GameObject) Resources.Load(entityData.resourceName), EntityManager.transform, true);
                    entityObject.transform.position = new Vector2(entityData.x, entityData.y);
                    var entity = entityObject.GetComponent<Entity>();
                    entity.id = entityData.id;
                    entity.dynamic = entityData.dynamic;
                    entity.generateCollider = entityData.generateCollider;
                    EntityManager.Entities.Add(entity.id, entity);
                    if (GlobalConfig.StaticGlobalConfig.levelDesignMode && !entity.dynamic)
                    {
                        EntityManager.QueueEntityRemoveFromMap(entity);
                    }
                    // show the sprite so we can move the entity
                    entity.spriteRenderer.enabled = true;
                }
            }
        }

        public void DrawRect(int x, int y, int width, int height, int type, Utils.Color color, int states, float lifetime)
        {
            var threadGroupsX = (int) Math.Ceiling((float) width / Chunk.Size) * 8;
            var threadGroupsY = (int) Math.Ceiling((float) height / Chunk.Size) * 8;
            if (threadGroupsX <= 0 || threadGroupsY <= 0)
                return;

            var worldWidth = Width * Chunk.Size;
            var worldHeight = Height * Chunk.Size;
            drawRectShader.SetBuffer(DrawRectHandle, "blocks", BlocksBuffer);
            drawRectShader.SetBuffer(DrawRectHandle, "indices", IndicesBuffer);
            drawRectShader.SetInts("rect", x, y, width, height);
            drawRectShader.SetInts("world_size", worldWidth, worldHeight);
            drawRectShader.SetInt("type", type);
            drawRectShader.SetFloats("color", color.r, color.g, color.b, color.a);
            // drawRectShader.SetFloats("color", color.r / 255f, color.g / 255f, color.b / 255f, color.a / 255f);
            drawRectShader.SetFloat("color_max_shift", color.MaxShift);
            drawRectShader.SetInt("states", states);
            drawRectShader.SetFloat("lifetime", lifetime);
            drawRectShader.SetTexture(DrawRectHandle, "colors", _textures[0]);
            drawRectShader.Dispatch(DrawRectHandle, threadGroupsX, threadGroupsY, 1);
            drawRectShader.SetTexture(DrawRectHandle, "colors", _textures[1]);
            drawRectShader.Dispatch(DrawRectHandle, threadGroupsX, threadGroupsY, 1);
        }

        // public Chunk GetChunk(Vector3 position)
        // {
        //     var chunkMap = ChunkManager.ChunkMap;
        //     return chunkMap.Contains(position) ? chunkMap[position] : null;
        // }
        //
        // public UnityEngine.Color[] GetColors()
        // {
        //     var rect = new Rect(0, 0, Size, Size);
        //
        //     RenderTexture.active = _chunkManager.RenderTexture;
        //
        //     _texture2D.ReadPixels(rect, 0, 0);
        //     _texture2D.Apply();
        //
        //     RenderTexture.active = null;
        //
        //     return _texture2D.GetPixels();
        // }

        // public byte[] GetColorsAsBytes()
        // {
        //     var colors = GetColors();
        //     var colorsAsBytes = new byte[colors.Length * 4];
        //     for (var i = 0; i < colors.Length; ++i)
        //     {
        //         colorsAsBytes[i * 4] = (byte) (colors[i].r * 255);
        //         colorsAsBytes[i * 4 + 1] = (byte) (colors[i].g * 255);
        //         colorsAsBytes[i * 4 + 2] = (byte) (colors[i].b * 255);
        //         colorsAsBytes[i * 4 + 3] = (byte) (colors[i].a * 255);
        //     }
        //
        //     return colorsAsBytes;
        // }
        //
        // public void SetColors(byte[] colorsAsBytes)
        // {
        //     _texture2D.LoadRawTextureData(colorsAsBytes);
        //     _texture2D.Apply();
        //
        //     _chunkManager.initColorsShader.SetTexture(_initColorsHandle, "initial_colors", _texture2D);
        //     _chunkManager.initColorsShader.SetTexture(_initColorsHandle, "colors", _chunkManager.RenderTexture);
        //     _chunkManager.initColorsShader.Dispatch(_initColorsHandle, 8, 8, 1);
        // }

        // This is not multi-platform compatible, not reliable and not called between scene loads
        private void OnApplicationQuit()
        {
            if (!GlobalConfig.StaticGlobalConfig.deleteSaveOnExit)
            {
                _tileTaskScheduler.CancelLoad();

                if (GlobalConfig.StaticGlobalConfig.levelDesignMode)
                    EntityManager.BlitStaticEntities();

                var tiles = _tileMap.Tiles().ToList();
                foreach (var tile in tiles)
                {
                    // QueueTileForSave(tile);
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

        private void OnDestroy()
        {
            BlocksBuffer.Release();
            IndicesBuffer.Release();
            foreach (var texture in _textures)
                texture.Release();
            TileBlocksBuffer.Release();
        }

        public struct lockIndex
        {
            public int index;
            public int lockValue;
        }

        #region Debug

        private void OutlineTiles()
        {
            var worldTileSize = tileGridThickness * 2 + 1;
            var width = worldTileSize * Tile.HorizontalChunks;
            var height = worldTileSize * Tile.VerticalChunks;
            var worldOffsetX = -0.5f - width / 2.0f;
            var worldOffsetY = -0.5f - height / 2.0f;
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

            var chunkManagerPosition = transform.position;
            var idx = 0;
            foreach (var tile in _tileMap.Tiles())
            {
                var tileColor = colorsPerIdx[idx];
                idx = (idx + 1) % 9;

                var x = chunkManagerPosition.x + tile.Position.x * Tile.HorizontalChunks;
                var y = chunkManagerPosition.y + tile.Position.y * Tile.VerticalChunks;

                // draw the tile borders
                Debug.DrawLine(new Vector3(x + worldOffsetX, y + worldOffsetY), new Vector3(x + worldOffsetX + Tile.HorizontalChunks, y + worldOffsetY), tileColor);
                Debug.DrawLine(new Vector3(x + worldOffsetX, y + worldOffsetY), new Vector3(x + worldOffsetX, y + worldOffsetY + Tile.VerticalChunks), tileColor);
                Debug.DrawLine(new Vector3(x + worldOffsetX + Tile.HorizontalChunks, y + worldOffsetY + Tile.VerticalChunks), new Vector3(x + worldOffsetX + Tile.HorizontalChunks, y + worldOffsetY), tileColor);
                Debug.DrawLine(new Vector3(x + worldOffsetX + Tile.HorizontalChunks, y + worldOffsetY + Tile.VerticalChunks), new Vector3(x + worldOffsetX, y + worldOffsetY + Tile.VerticalChunks), tileColor);
            }
        }

        #endregion
    }
}
