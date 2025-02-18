﻿using System;
using Chunks.Server;
using Serialized;
using Utils;

namespace Blocks.Behaviors
{
    public class PlantGrower : IBehavior
    {
        public readonly float XInitialGrowthDirection;
        public readonly float YInitialGrowthDirection;
        public readonly int MaximumDepthLevels;
        private readonly int _minimumTicksBeforeGrowth;
        private readonly int _maximumTickBeforeGrowth;
        private readonly float _branchProbability;
        private readonly float[] _growthDirectionVariationProbabilitiesPerDepthLevel;
        private readonly int[] _minimumDistancesPerDepthLevel;
        private readonly int[] _maximumDistancesPerDepthLevel;
        private readonly int[] _growthMediumBlocks;
        private readonly int _rootType;

        public PlantGrower(float xInitialGrowthDirection,
            float yInitialGrowthDirection,
            int maximumDepthLevels,
            float branchProbability,
            float[] growthDirectionVariationProbabilitiesPerDepthLevel,
            int[] minimumDistancesPerDepthLevel,
            int[] maximumDistancesPerDepthLevel,
            int minimumTicksBeforeGrowth,
            int maximumTicksBeforeGrowth,
            int[] growthMediumBlocks,
            int rootType)
        {
            XInitialGrowthDirection = xInitialGrowthDirection;
            YInitialGrowthDirection = yInitialGrowthDirection;
            MaximumDepthLevels = maximumDepthLevels;
            _branchProbability = branchProbability;
            _growthDirectionVariationProbabilitiesPerDepthLevel = growthDirectionVariationProbabilitiesPerDepthLevel;
            _minimumDistancesPerDepthLevel = minimumDistancesPerDepthLevel;
            _maximumDistancesPerDepthLevel = maximumDistancesPerDepthLevel;
            _minimumTicksBeforeGrowth = minimumTicksBeforeGrowth;
            _maximumTickBeforeGrowth = maximumTicksBeforeGrowth;
            _growthMediumBlocks = growthMediumBlocks;
            _rootType = rootType;
        }

        public bool Execute(Random rng, ChunkServerNeighborhood chunkNeighborhood, ref Block block, int x, int y)
        {
            ref var self = ref chunkNeighborhood.GetCentralChunk().GetPlantBlockData(x, y, block.type);
            if (self.id == 0)
                self.Reset(block.type, UniqueIdGenerator.Next());

            self.ticksBeforeGrowth--;

            if (self.ticksBeforeGrowth > 0 || self.growthCount >= 1)
                return true;

            if (self.depthLevel >= MaximumDepthLevels)
                return false;

            if (self.distanceToRoot >= rng.Next(_minimumDistancesPerDepthLevel[self.depthLevel],
                _maximumDistancesPerDepthLevel[self.depthLevel]))
            {
                self.growthCount++;
                return false;
            }

            TryGrowingRoot(self, x, y, chunkNeighborhood, rng);

            var gx = self.xGrowthDirection;
            var gy = self.yGrowthDirection;
            var depth = self.depthLevel;
            var growthCountIncrement = 1;
            var distanceToRoot = self.distanceToRoot;

            var branching = false;
            if (_branchProbability >= rng.NextDouble())
            {
                // branch to the right
                if (rng.NextDouble() < 0.5)
                {
                    gx = self.yGrowthDirection;
                    gy = -self.xGrowthDirection + 0.5f;
                }
                // branch to the left
                else
                {
                    gx = -self.yGrowthDirection;
                    gy = self.xGrowthDirection + 0.5f;
                }

                distanceToRoot = 0;
                growthCountIncrement = 0;
                depth++;
                branching = true;
            }

            var newXGrowthDirection = gx;
            var newYGrowthDirection = gy;
            if (!branching)
            {
                if (gx < 1.0f && gx > -1.0f)
                {
                    gx = Math.Abs(gx) < rng.NextDouble() ? 0.0f : gx < 0.0f ? -1.0f : 1.0f;
                    newXGrowthDirection = _growthDirectionVariationProbabilitiesPerDepthLevel[self.depthLevel];
                    if (rng.Next(0, 2) == 0)
                        newXGrowthDirection = -newXGrowthDirection;
                }
                else if (gy < 1.0f && gy > -1.0f)
                {
                    gy = Math.Abs(gy) < rng.NextDouble() ? 0.0f : gy < 0.0f ? -1.0f : 1.0f;
                    newYGrowthDirection = _growthDirectionVariationProbabilitiesPerDepthLevel[self.depthLevel];
                    if (rng.NextDouble() < 0.5)
                        newYGrowthDirection = -newYGrowthDirection;
                }
            }
            var nx = x + (int) gx;
            var ny = y + (int) gy;
            var targetBlockType = chunkNeighborhood.GetBlockType(nx, ny);
            if (CanGrowInto(targetBlockType) || targetBlockType == block.type)
            {
                chunkNeighborhood.ReplaceBlock(nx, ny, block.type, block.states, block.health, block.lifetime, block.entityId);
                ref var newBlock = ref chunkNeighborhood.GetPlantBlockData(nx, ny, block.type);
                newBlock.Reset(block.type, self.id);
                newBlock.ticksBeforeGrowth = rng.Next(_minimumTicksBeforeGrowth, _maximumTickBeforeGrowth + 1);
                newBlock.xGrowthDirection = newXGrowthDirection;
                newBlock.yGrowthDirection = newYGrowthDirection;
                newBlock.depthLevel = depth;
                newBlock.distanceToRoot = distanceToRoot + 1;
            }

            if (!branching)
                self.growthCount += growthCountIncrement;

            self.ticksBeforeGrowth = rng.Next(_minimumTicksBeforeGrowth, _maximumTickBeforeGrowth + 1);
            return true;
        }

        private void TryGrowingRoot(PlantBlockData self, int x, int y, ChunkServerNeighborhood chunkNeighborhood, Random rng)
        {
            if (_rootType == BlockConstants.Border || self.distanceToRoot != 0 || self.depthLevel != 0)
                return;

            // grow root in the opposite direction
            var gx = -self.xGrowthDirection;
            var gy = -self.yGrowthDirection;

            var nx = x + (int) gx;
            var ny = y + (int) gy;
            var rootDescriptor = BlockConstants.BlockDescriptors[_rootType];
            var rootGrower = rootDescriptor.PlantGrower;
            var targetBlock = chunkNeighborhood.GetBlockType(nx, ny);
            if (rootGrower.CanGrowInto(targetBlock) && targetBlock != _rootType)
            {
                chunkNeighborhood.ReplaceBlock(nx, ny, _rootType, 0, rootDescriptor.BaseHealth, 0, 0);
                ref var newBlock = ref chunkNeighborhood.GetPlantBlockData(nx, ny, _rootType);
                newBlock.Reset(_rootType, self.id);
                newBlock.ticksBeforeGrowth = rng.Next(rootGrower._minimumTicksBeforeGrowth, rootGrower._maximumTickBeforeGrowth + 1);
                newBlock.xGrowthDirection = gx;
                newBlock.yGrowthDirection = gy;
                newBlock.depthLevel = 0;
                newBlock.distanceToRoot = 0;
            }
        }

        private bool CanGrowInto(int targetBlock)
        {
            foreach (var t in _growthMediumBlocks)
            {
                if (t == targetBlock)
                    return true;
            }

            return false;
        }
    }
}
