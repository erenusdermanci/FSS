using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Chunks.Server;
using Chunks.Tasks;
using Tools;
using Utils;

namespace Chunks
{
    public class ChunkLayerSimulator
    {
        private const int BatchNumber = 4;
        private readonly List<ConcurrentDictionary<Vector2i, SimulationTask>> _simulationBatchPool =
            new List<ConcurrentDictionary<Vector2i, SimulationTask>>(BatchNumber);

        private readonly ChunkLayer _chunkLayer;

        public event EventHandler Simulated;

        public ChunkLayerSimulator(ChunkLayer chunkLayer)
        {
            _chunkLayer = chunkLayer;
            for (var i = 0; i < BatchNumber; ++i)
            {
                _simulationBatchPool.Add(new ConcurrentDictionary<Vector2i, SimulationTask>());
            }
        }

        public void Update()
        {
            var enableDirty = !GlobalDebugConfig.StaticGlobalConfig.disableDirtyChunks;

            foreach (var batch in _simulationBatchPool)
            {
                foreach (var task in batch.Values)
                {
                    if (enableDirty && !task.Chunk.Dirty)
                        continue;
                    task.Schedule();
                }

                foreach (var task in batch.Values)
                {
                    if (!task.Scheduled())
                        continue;
                    task.Join();
                    Simulated?.Invoke(this, new ChunkTaskEvent<ChunkServer>(task.Chunk));
                }
            }
        }

        public void Clear()
        {
            foreach (var batch in _simulationBatchPool)
            {
                batch.Clear();
            }
        }

        public void UpdateSimulationPool(ChunkServer chunk, bool add)
        {
            var chunkPos = chunk.Position;
            var batchIndex = GetChunkBatchIndex(chunkPos);
            if (add)
            {
                if (_simulationBatchPool[batchIndex].ContainsKey(chunkPos))
                    return; // this chunk simulation task already exists
                var task = new SimulationTask(chunk, _chunkLayer.type)
                {
                    ChunkNeighborhood = new ChunkServerNeighborhood(_chunkLayer.ServerChunkMap, chunk)
                };
                _simulationBatchPool[batchIndex].TryAdd(chunkPos, task);
            }
            else
            {
                _simulationBatchPool[batchIndex].TryRemove(chunkPos, out _);
            }

            // update all chunks tasks that need or had me as a neighbour
            UpdateNeighborhoodsInNeighborChunks(chunk);
        }

        private void UpdateNeighborhoodsInNeighborChunks(ChunkServer chunk)
        {
            for (var y = -1; y < 2; y++)
            {
                for (var x = -1; x < 2; x++)
                {
                    if (y == 0 && x == 0)
                        continue;
                    var neighbor = ChunkHelpers.GetNeighborChunk(_chunkLayer.ServerChunkMap, chunk, x, y);
                    if (neighbor != null)
                    {
                        neighbor.ResetDirty();
                        // find the neighbor in the batch pool
                        var neighborBatchIndex = GetChunkBatchIndex(neighbor.Position);
                        if (_simulationBatchPool[neighborBatchIndex].ContainsKey(neighbor.Position))
                        {
                            var neighborTask = _simulationBatchPool[neighborBatchIndex][neighbor.Position];

                            // TODO: ideally we should only update the correct neighbor, but I'm being lazy here
                            // and its not the worst strain on performance
                            neighborTask.ChunkNeighborhood.UpdateNeighbors(_chunkLayer.ServerChunkMap, neighbor);
                        }
                    }
                }
            }
        }

        public static int GetChunkBatchIndex(Vector2i position)
        {
            // small bitwise trick to find the batch index and avoid an ugly forest
            return ((Math.Abs(position.x) % 2) << 1)
                   | (Math.Abs(position.y) % 2);
        }
    }
}
