using System;
using System.Collections.Generic;
using Chunks.Client;
using Chunks.Server;
using Chunks.Tasks;
using Tools;
using Tiles;
using UnityEngine;
using Utils;
using Utils.UnityHelpers;
using Color = UnityEngine.Color;

namespace Chunks
{
    public class ChunkLayer : MonoBehaviour
    {
        public enum ChunkLayerType
        {
            Foreground,
            Background
        }

        public ChunkLayerType type;

        public readonly ChunkMap<ChunkServer> ServerChunkMap = new ChunkMap<ChunkServer>();
        public readonly ChunkMap<ChunkClient> ClientChunkMap = new ChunkMap<ChunkClient>();
        private readonly List<ChunkClient> _chunksToRender = new List<ChunkClient>();

        private ChunkServerTaskScheduler _chunkServerTaskScheduler;
        public ChunkLayerSimulator chunkSimulator;

        private bool _userPressedSpace;

        public void Awake()
        {
            _chunkServerTaskScheduler = new ChunkServerTaskScheduler(type);
            chunkSimulator = new ChunkLayerSimulator(this);
        }

        public void Start()
        {
            _chunkServerTaskScheduler.GetTaskManager(ChunkTaskTypes.Generate).Processed += OnChunkGenerated;
            chunkSimulator.Simulated += OnChunkSimulated;
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

                if (GlobalDebugConfig.StaticGlobalConfig.hideCleanChunkOutlines && !chunk.Dirty)
                    continue;

                // draw the chunk borders
                var borderColor = chunk.Dirty ? Color.red : Color.white;
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x + worldOffset, y - worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x - worldOffset, y - worldOffset), new Vector3(x - worldOffset, y + worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x + worldOffset, y + worldOffset), new Vector3(x - worldOffset, y + worldOffset), borderColor);
                Debug.DrawLine(new Vector3(x + worldOffset, y + worldOffset), new Vector3(x + worldOffset, y - worldOffset), borderColor);
            }
        }

        private void FinalizeChunkCreation(ChunkServer chunk)
        {
            ServerChunkMap.Add(chunk);

            // CreateClientChunk(chunk);

            chunkSimulator.UpdateSimulationPool(chunk, true);
        }

        public void Update()
        {
            _chunkServerTaskScheduler.Update();

            if (Input.GetKeyDown(KeyCode.Space))
                _userPressedSpace = true;
        }

        private void FixedUpdate()
        {
            // if (worldManager.CameraHasMoved)
            // {
            //     Generate(worldManager.CameraFlooredPosition); // replace with load
            //     Clean(worldManager.CameraFlooredPosition); // replace with save+unload
            // }

            if (GlobalDebugConfig.StaticGlobalConfig.stepByStep && _userPressedSpace)
            {
                _userPressedSpace = false;
                chunkSimulator.Update();
            }
            else if (!GlobalDebugConfig.StaticGlobalConfig.pauseSimulation)
                chunkSimulator.Update();

            RenderChunks();

            if (GlobalDebugConfig.StaticGlobalConfig.outlineChunks)
                OutlineChunks();

            if (!GlobalDebugConfig.StaticGlobalConfig.disableDirtyRects && GlobalDebugConfig.StaticGlobalConfig.drawDirtyRects)
                DrawDirtyRects();
        }

        // private void Clean(Vector2i aroundPosition)
        // {
        //     var px = aroundPosition.x - (float)worldManager.generatedAreaSize / 2;
        //     var py = aroundPosition.y - (float)worldManager.generatedAreaSize / 2;
        //
        //     var chunksToRemove = new List<Vector2i>();
        //     foreach (var chunk in ServerChunkMap.Chunks())
        //     {
        //         if (!(chunk.Position.x < px - worldManager.cleanAreaSizeOffset) &&
        //             !(chunk.Position.x > px + worldManager.generatedAreaSize + worldManager.cleanAreaSizeOffset) &&
        //             !(chunk.Position.y < py - worldManager.cleanAreaSizeOffset) &&
        //             !(chunk.Position.y > py + worldManager.generatedAreaSize + worldManager.cleanAreaSizeOffset)) continue;
        //         chunksToRemove.Add(chunk.Position);
        //     }
        //
        //     foreach (var chunkPosition in chunksToRemove)
        //     {
        //         var chunk = ServerChunkMap[chunkPosition];
        //         ServerChunkMap.Remove(chunkPosition);
        //         DisposeChunk(chunk);
        //     }
        // }

        private void DisposeChunk(ChunkServer chunk)
        {
            ServerChunkMap[chunk.Position]?.Dispose();
            ServerChunkMap.Remove(chunk.Position);
            ClientChunkMap[chunk.Position]?.Dispose();
            ClientChunkMap.Remove(chunk.Position);
            chunkSimulator.UpdateSimulationPool(chunk, false);
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
            _chunkServerTaskScheduler.CancelGeneration();

            foreach (var chunk in ServerChunkMap.Chunks())
            {
                DisposeChunk(chunk);
            }

            ServerChunkMap.Clear();
        }
    }
}
