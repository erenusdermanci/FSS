using System;
using System.Collections.Generic;
using Chunks.Client;
using Chunks.Server;
using Chunks.Tasks;
using Tiles;
using Tools;
using UnityEngine;
using Utils;
using Color = UnityEngine.Color;

namespace Chunks
{
    public class ChunkLayer : MonoBehaviour
    {
        public static readonly int TotalChunkLayers = Enum.GetValues(typeof(ChunkLayerType)).Length;

        public ChunkLayerType type;

        private WorldManager _worldManager;

        public readonly ChunkMap<ChunkServer> ServerChunkMap = new ChunkMap<ChunkServer>();
        public readonly ChunkMap<ChunkClient> ClientChunkMap = new ChunkMap<ChunkClient>();
        private readonly List<ChunkClient> _chunksToRender = new List<ChunkClient>();
        private readonly HashSet<Vector2i> _chunksToReload = new HashSet<Vector2i>();

        public ChunkLayerSimulator chunkSimulator;

        private bool _userPressedSpace;

        public void Awake()
        {
            _worldManager = transform.parent.GetComponent<WorldManager>();
            chunkSimulator = new ChunkLayerSimulator(this);
        }

        public void Start()
        {
            chunkSimulator.Simulated += OnChunkSimulated;
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

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _userPressedSpace = true;

            foreach (var chunkPosition in _chunksToReload)
            {
                var serverChunk = ServerChunkMap[chunkPosition];
                if (serverChunk != null)
                {
                    var chunkDirtyRects = serverChunk.DirtyRects;
                    for (var i = 0; i < chunkDirtyRects.Length; ++i)
                    {
                        chunkDirtyRects[i].Reset();
                        chunkDirtyRects[i].Initialized = false;
                    }

                    serverChunk.Dirty = true;
                }

                var clientChunk = ClientChunkMap[chunkPosition];
                if (clientChunk != null)
                {
                    if (type == ChunkLayerType.Foreground)
                        _worldManager.CollisionManager.QueueChunkCollisionGeneration(serverChunk);
                    clientChunk.UpdateTexture();
                }
            }
            _chunksToReload.Clear();
        }

        private void FixedUpdate()
        {
            if (!GlobalConfig.StaticGlobalConfig.levelDesignMode)
            {
                if (GlobalConfig.StaticGlobalConfig.stepByStep && _userPressedSpace)
                {
                    _userPressedSpace = false;
                    chunkSimulator.Update();
                }
                else if (!GlobalConfig.StaticGlobalConfig.pauseSimulation)
                    chunkSimulator.Update();
            }

            if (GlobalConfig.StaticGlobalConfig.outlineChunks)
                OutlineChunks();

            RenderChunks();

            if (!GlobalConfig.StaticGlobalConfig.disableDirtyRects && GlobalConfig.StaticGlobalConfig.drawDirtyRects)
                DrawDirtyRects();
        }

        public void QueueChunkForReload(Vector2i chunkPosition)
        {
            _chunksToReload.Add(chunkPosition);
        }

        private void RenderChunks()
        {
            foreach (var chunkClient in _chunksToRender)
            {
                chunkClient.UpdateTexture();
            }
            _chunksToRender.Clear();
        }

        #region Debug

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

        private void OutlineChunks()
        {
            const float worldOffset = 0.5f;
            foreach (var chunk in ServerChunkMap.Chunks())
            {
                var x = chunk.Position.x;
                var y = chunk.Position.y;
                var mapBorderColor = Color.white;

                // draw the map borders
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x - 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset, y + worldOffset), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x + 1, chunk.Position.y)))
                    Debug.DrawLine(new Vector3(x + worldOffset, y - worldOffset), new Vector3(x + worldOffset, y + worldOffset), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y - 1)))
                    Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x + worldOffset, y - worldOffset), mapBorderColor);
                if (!ServerChunkMap.Contains(new Vector2i(chunk.Position.x, chunk.Position.y + 1)))
                    Debug.DrawLine(new Vector3(x - worldOffset, y + worldOffset), new Vector3(x + worldOffset, y + worldOffset), mapBorderColor);

                if (GlobalConfig.StaticGlobalConfig.hideCleanChunkOutlines && !chunk.Dirty)
                    continue;

                // draw the chunk borders
                var borderColor = chunk.Dirty ? Color.red : Color.white;
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x + worldOffset, y - worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset, y + worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x + worldOffset, y + worldOffset), new Vector3(x - worldOffset, y + worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x + worldOffset, y + worldOffset), new Vector3(x + worldOffset, y - worldOffset), borderColor);
            }
        }

        #endregion

        public string GetChunkSortingLayer()
        {
            switch (type)
            {
                case ChunkLayerType.Foreground:
                    return "Foreground";
                case ChunkLayerType.Background:
                    return "Background";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
