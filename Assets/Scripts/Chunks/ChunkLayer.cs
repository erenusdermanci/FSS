using System;
using System.Collections.Generic;
using Chunks.Client;
using Chunks.Server;
using Chunks.Tasks;
using DebugTools;
using ProceduralGeneration;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

namespace Chunks
{
    public class ChunkLayer : MonoBehaviour
    {
        public ChunkManager chunkManager;
        private GameObjectPool _chunkPool;

        public enum ChunkLayerType
        {
            Foreground,
            Background
        }

        public ChunkLayerType type;

        public ClientCollisionManager ClientCollisionManager;

        public readonly ChunkMap<ChunkServer> ServerChunkMap = new ChunkMap<ChunkServer>();
        public readonly ChunkMap<ChunkClient> ClientChunkMap = new ChunkMap<ChunkClient>();
        private readonly List<ChunkClient> _chunksToRender = new List<ChunkClient>();

        private ChunkServerTaskScheduler _chunkServerTaskScheduler;
        private ChunkLayerSimulator _chunkSimulator;

        private bool _userPressedSpace;

        public void Awake()
        {
            _chunkServerTaskScheduler = new ChunkServerTaskScheduler(type);
            _chunkSimulator = new ChunkLayerSimulator(this);

            switch (type)
            {
                case ChunkLayerType.Foreground:
                    ClientCollisionManager = new ClientCollisionManager(this);
                    break;
                case ChunkLayerType.Background:
                    // no collisions on the background
                    break;
            }

            ProceduralGenerator.UpdateEvent += ProceduralGeneratorUpdate;
        }

        public void Start()
        {
            _chunkPool = new GameObjectPool(this, chunkManager.generatedAreaSize * chunkManager.generatedAreaSize);
            chunkManager.GeneratedAreaSizeChanged += OnGeneratedAreaSizeChanged;
            _chunkServerTaskScheduler.GetTaskManager(ChunkTaskTypes.Save).Processed += OnChunkSaved;
            _chunkServerTaskScheduler.GetTaskManager(ChunkTaskTypes.Load).Processed += OnChunkLoaded;
            _chunkServerTaskScheduler.GetTaskManager(ChunkTaskTypes.Generate).Processed += OnChunkGenerated;
            _chunkSimulator.Simulated += OnChunkSimulated;
        }

        private void OnChunkSaved(object sender, EventArgs e)
        {
            var chunk = ((ChunkTaskEvent<ChunkServer>) e).Chunk;
            ServerChunkMap[chunk.Position]?.Dispose();
            ServerChunkMap.Remove(chunk.Position);
            ClientChunkMap[chunk.Position]?.Dispose();
            ClientChunkMap.Remove(chunk.Position);
            _chunkSimulator.UpdateSimulationPool(chunk, false);
        }

        private void OnChunkLoaded(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskEvent<ChunkServer>) e).Chunk);
        }

        private void OnChunkGenerated(object sender, EventArgs e)
        {
            FinalizeChunkCreation(((ChunkTaskEvent<ChunkServer>) e).Chunk);
        }

        private void OnChunkSimulated(object sender, EventArgs e)
        {
            var chunk = ((ChunkTaskEvent<ChunkServer>) e).Chunk;
            var clientChunk = ClientChunkMap[chunk.Position];
            if (clientChunk == null)
                return;
            clientChunk.Dirty = chunk.Dirty;
            _chunksToRender.Add(clientChunk);
        }

        private void OnGeneratedAreaSizeChanged(object sender, EventArgs e)
        {
            ResetGrid(true);
        }

        private void ResetGrid(bool loadFromDisk)
        {
            var position = chunkManager.playerTransform.position;
            var flooredAroundPosition = new Vector2i((int) Mathf.Floor(position.x), (int) Mathf.Floor(position.y));

            _chunkServerTaskScheduler.CancelLoading();
            _chunkServerTaskScheduler.CancelGeneration();

            foreach (var clientChunk in ClientChunkMap.Map.Values)
            {
                clientChunk.Dispose();
            }
            ClientChunkMap.Clear();

            foreach (var serverChunk in ServerChunkMap.Map.Values)
            {
                serverChunk.Dispose();
            }
            ServerChunkMap.Clear();

            _chunkSimulator.Clear();

            Generate(flooredAroundPosition, loadFromDisk);
        }

        private void ProceduralGeneratorUpdate(object sender, EventArgs e)
        {
            ResetGrid(false);
        }

        private void OutlineChunks()
        {
            const float s = 0.5f;
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                var x = chunk.Position.x;
                var y = chunk.Position.y;
                var mapBorderColor = Color.white;

                // draw the map borders
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x - 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x + 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x + s, y - s), new Vector3(x + s, y + s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y - 1)))
                    Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y + 1)))
                    Debug.DrawLine(new Vector3(x - s, y + s), new Vector3(x + s, y + s), mapBorderColor);

                if (GlobalDebugConfig.StaticGlobalConfig.hideCleanChunkOutlines && !chunk.Dirty)
                    continue;

                // draw the chunk borders
                var borderColor = chunk.Dirty ? Color.red : Color.white;
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x + s, y - s), borderColor);
                Debug.DrawLine(new Vector3(x - s, y - s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x - s, y + s), borderColor);
                Debug.DrawLine(new Vector3(x + s, y + s), new Vector3(x + s, y - s), borderColor);
            }
        }

        private void FinalizeChunkCreation(ChunkServer chunk)
        {
            ServerChunkMap.Add(chunk);

            CreateClientChunk(chunk);

            _chunkSimulator.UpdateSimulationPool(chunk, true);
        }

        public void Update()
        {
            _chunkServerTaskScheduler.Update();

            if (Input.GetKeyDown(KeyCode.Space))
                _userPressedSpace = true;
        }

        private void FixedUpdate()
        {
            if (chunkManager.PlayerHasMoved)
            {
                Generate(chunkManager.PlayerFlooredPosition, true);
                Clean(chunkManager.PlayerFlooredPosition);
            }

            if (GlobalDebugConfig.StaticGlobalConfig.stepByStep && _userPressedSpace)
            {
                _userPressedSpace = false;
                _chunkSimulator.Update();
            }
            else if (!GlobalDebugConfig.StaticGlobalConfig.pauseSimulation)
                _chunkSimulator.Update();

            RenderChunks();

            if (GlobalDebugConfig.StaticGlobalConfig.outlineChunks)
                OutlineChunks();

            if (!GlobalDebugConfig.StaticGlobalConfig.disableDirtyRects && GlobalDebugConfig.StaticGlobalConfig.drawDirtyRects)
                DrawDirtyRects();

            if (!GlobalDebugConfig.StaticGlobalConfig.disableCollisions)
                ClientCollisionManager?.GenerateCollisions(chunkManager.PlayerFlooredPosition, chunkManager.PlayerHasMoved);
        }

        private void Clean(Vector2i aroundPosition)
        {
            var px = aroundPosition.x - (float)chunkManager.generatedAreaSize / 2;
            var py = aroundPosition.y - (float)chunkManager.generatedAreaSize / 2;

            var chunksToRemove = new List<Vector2i>();
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                if (!(chunk.Position.x < px - chunkManager.cleanAreaSizeOffset) &&
                    !(chunk.Position.x > px + chunkManager.generatedAreaSize + chunkManager.cleanAreaSizeOffset) &&
                    !(chunk.Position.y < py - chunkManager.cleanAreaSizeOffset) &&
                    !(chunk.Position.y > py + chunkManager.generatedAreaSize + chunkManager.cleanAreaSizeOffset)) continue;
                chunksToRemove.Add(chunk.Position);
            }

            foreach (var chunkPosition in chunksToRemove)
            {
                var chunk = ServerChunkMap[chunkPosition];
                ServerChunkMap.Remove(chunkPosition);
                DisposeAndSaveChunk(chunk);
            }
        }

        private void DisposeAndSaveChunk(ChunkServer chunk)
        {
            if (GlobalDebugConfig.StaticGlobalConfig.disableSave)
            {
                ServerChunkMap[chunk.Position]?.Dispose();
                ServerChunkMap.Remove(chunk.Position);
                ClientChunkMap[chunk.Position]?.Dispose();
                ClientChunkMap.Remove(chunk.Position);
                _chunkSimulator.UpdateSimulationPool(chunk, false);
                return;
            }
            _chunkServerTaskScheduler.QueueForSaving(chunk);
        }

        private void CreateClientChunk(ChunkServer serverChunk)
        {
            var chunkGameObject = _chunkPool.GetObject();
            chunkGameObject.transform.position = new Vector3(serverChunk.Position.x, serverChunk.Position.y, 0);
            var clientChunk = new ChunkClient
            {
                Position = serverChunk.Position,
                Colors = serverChunk.Data.colors, // pointer on ChunkServer colors,
                Types = serverChunk.Data.types, // pointer on ChunkServer types,
                GameObject = chunkGameObject,
                Collider = chunkGameObject.GetComponent<PolygonCollider2D>(),
                Texture = chunkGameObject.GetComponent<SpriteRenderer>().sprite.texture
            };
            chunkGameObject.SetActive(true);
            ClientChunkMap.Add(clientChunk);
            clientChunk.UpdateTexture();
        }

        private void Generate(Vector2i aroundPosition, bool loadFromDisk)
        {
            for (var x = 0; x < chunkManager.generatedAreaSize; ++x)
            {
                for (var y = 0; y < chunkManager.generatedAreaSize; ++y)
                {
                    var pos = new Vector2i(aroundPosition.x + (x - chunkManager.generatedAreaSize / 2), aroundPosition.y + (y - chunkManager.generatedAreaSize / 2));
                    if (ServerChunkMap.Contains(pos))
                        continue;
                    _chunkServerTaskScheduler.QueueForGeneration(pos, loadFromDisk);
                }
            }
        }

        private void DrawDirtyRects()
        {
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                if (!chunk.Dirty)
                    continue;

                var x = chunk.Position.x;
                var y = chunk.Position.y;

                var chunkBatchIndex = ChunkLayerSimulator.GetChunkBatchIndex(chunk.Position);
                Color32 dirtyRectColor;
                switch (chunkBatchIndex)
                {
                    case 0:
                        dirtyRectColor = Color.green / ((int)type + 1.0f);
                        break;
                    case 1:
                        dirtyRectColor = Color.red / ((int)type + 1.0f);
                        break;
                    case 2:
                        dirtyRectColor = Color.magenta / ((int)type + 1.0f);
                        break;
                    case 3:
                        dirtyRectColor = Color.yellow / ((int)type + 1.0f);
                        break;
                    default:
                        return;
                }
                for (var i = 0; i < chunk.DirtyRects.Length; ++i)
                {
                    if (chunk.DirtyRects[i].X < 0.0f)
                        continue;
                    var rx = x - 0.5f + (chunk.DirtyRects[i].X + ChunkServer.DirtyRectX[i]) / (float)Chunk.Size;
                    var rxMax = x - 0.5f + (chunk.DirtyRects[i].XMax + ChunkServer.DirtyRectX[i] + 1) / (float)Chunk.Size;
                    var ry = y - 0.5f + (chunk.DirtyRects[i].Y + ChunkServer.DirtyRectY[i]) / (float)Chunk.Size;
                    var ryMax = y - 0.5f + (chunk.DirtyRects[i].YMax + ChunkServer.DirtyRectY[i] + 1) / (float)Chunk.Size;
                    Debug.DrawLine(new Vector3(rx, ry), new Vector3(rxMax, ry), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rx, ry), new Vector3(rx, ryMax), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rxMax, ry), new Vector3(rxMax, ryMax), dirtyRectColor);
                    Debug.DrawLine(new Vector3(rx, ryMax), new Vector3(rxMax, ryMax), dirtyRectColor);
                }
            }
        }

        private void RenderChunks()
        {
            foreach (var chunkClient in _chunksToRender)
            {
                chunkClient.UpdateTexture();
            }
            _chunksToRender.Clear();
        }

        private void OnDestroy()
        {
            _chunkServerTaskScheduler.CancelLoading();
            _chunkServerTaskScheduler.CancelGeneration();

            foreach (var chunk in ServerChunkMap.Chunks())
            {
                DisposeAndSaveChunk(chunk);
            }

            _chunkServerTaskScheduler.ForceSaving();

            ServerChunkMap.Clear();
        }
    }
}