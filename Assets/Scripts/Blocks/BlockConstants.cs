using System.Linq;
using Blocks.Behaviors;
using Utils;
using static Blocks.BlockMovementType;

namespace Blocks
{
    // directions
    // 0 -> down
    // 1 -> downLeft
    // 2 -> downRight
    // 3 -> left
    // 4 -> right
    // 5 -> up
    // 6 -> upLeft
    // 7 -> upRight

    public static class BlockConstants
    {
        public const int UnassignedBlockType = -1;
        private static readonly Color UnassignedBlockColor = new Color(255, 0, 0, 255);

        private static readonly Color FireColor = new Color(240, 127, 19, 255, 0.3f);

        public const int Air = 0;
        public const int Flame = 1;
        public const int Oil = 2;
        public const int Water = 3;
        public const int Sand = 4;
        public const int Dirt = 5;
        public const int Stone = 6;
        public const int Metal = 7;
        public const int Border = 8;
        public const int Gas = 9;
        public const int Smoke = 10;
        public const int Wood = 11;
        public const int Coal = 12;
        public const int Spark = 13;
        public const int Lava = 14;
        public const int HardenedLava = 15;
        public const int Plant = 16;
        public const int PlantRoot = 17;
        public const int Grass = 18;
        public const int NumberOfBlocks = 19;

        private static readonly FireSpreader PlantFireSpreader = new FireSpreader(
            1.0f,
            FireColor,
            5.0f,
            new[]
            {
                new BlockPotential(Flame, 1.0f)
            },
            new BlockPotential(Smoke, 1.0f),
            true,
            false
        );

        public static readonly BlockDescriptor[] BlockDescriptors = {
            new BlockDescriptor(
                "Air",
                BlockTags.NonPhysical,
                1.0f,
                new Color(0, 0, 0, 0),
                0.0f,
                0,
                new IBehavior[] {}
            ),
            new BlockDescriptor(
                "Flame",
                BlockTags.Gas,
                0.06f,
                new Color(255, 110, 19, 255, 0.2f),
                100.0f,
                1,
                new IBehavior[]
                {
                    new Despawner(0.2f,
                        10.0f,
                        new BlockPotential(Smoke, 0.2f)
                    ),
                    new FireSpreader(0.0f,
                        FireColor,
                        0.0f,
                        new BlockPotential[] {},
                        null,
                        false,
                        false
                    ),
                    new Swapper(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 1, 1, 1, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor (
                "Oil",
                BlockTags.Liquid,
                1.1f,
                new Color(51, 38, 0, 255, 0.1f),
                100.0f,
                0,
                new IBehavior[]
                {
                    new FireSpreader(0.1f,
                        FireColor,
                        0.01f,
                        new [] { new BlockPotential(Flame, 1.0f) },
                        new BlockPotential(Smoke, 0.5f),
                        true,
                        false),
                    new Swapper(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Farthest, Farthest, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor (
                "Water",
                BlockTags.Liquid,
                1.2f,
                new Color(15, 94, 156, 125, 0.025f),
                0.0f,
                0,
                new IBehavior[]
                {
                    new Swapper(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Farthest, Farthest, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor (
                "Sand",
                BlockTags.Solid,
                1.5f,
                new Color(155, 134, 69, 255, 0.05f),
                0.0f,
                0,
                new IBehavior[]
                {
                    new Swapper(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor (
                "Dirt",
                BlockTags.Solid,
                1.6f,
                new Color(124, 94, 66, 255, 0.1f),
                0.0f,
                0,
                new IBehavior[] {}
            ),
            new BlockDescriptor (
                "Stone",
                BlockTags.Solid,
                2.0f,
                new Color(149, 148, 139, 255, 0.2f),
                0.0f,
                0,
                new IBehavior[] {}
            ),
            new BlockDescriptor (
                "Metal",
                BlockTags.Solid,
                2.0f,
                new Color(75, 75, 75, 255, 0.02f),
                0.0f,
                0,
                new IBehavior[] {}
            ),
            new BlockDescriptor (
                "Border",
                BlockTags.Solid,
                1000.0f,
                new Color(255, 0, 0, 255),
                0.0f,
                0,
                new IBehavior[] {}
            ),
            new BlockDescriptor(
                "Gas",
                BlockTags.Gas,
                0.05f,
                new Color(73, 185, 96, 255, 0.1f),
                100.0f,
                0,
                new IBehavior[]
                {
                    new FireSpreader(1.0f,
                        FireColor,
                        10.0f,
                        new BlockPotential [] {},
                        new BlockPotential(Smoke, 0.25f),
                        false,
                        false
                    ),
                    new Swapper(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor(
                "Smoke",
                BlockTags.Gas,
                0.06f,
                new Color(59, 68, 75, 75, 0.5f),
                0.0f,
                0,
                new IBehavior[]
                {
                    new Despawner(0.1f,
                        100.0f,
                        new BlockPotential(Air, 1.0f)
                    ),
                    new Swapper(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor(
                "Wood",
                BlockTags.Solid,
                2.0f,
                new Color(99, 70, 45, 255, 0.1f),
                100.0f,
                0,
                new IBehavior[] {
                    new FireSpreader(0.8f,
                        FireColor,
                        3.0f,
                        new [] {
                            new BlockPotential(Spark, 0.12f),
                            new BlockPotential(Flame, 1.0f)
                        },
                        new BlockPotential(Coal, 0.1f),
                        true,
                        false
                    )
                }
            ),
            new BlockDescriptor(
                "Coal",
                BlockTags.Solid,
                2.0f,
                new Color(50, 50, 50, 255, 0.4f),
                100.0f,
                0,
                new IBehavior[] {
                    new FireSpreader(0.2f,
                        FireColor,
                        0.01f,
                        new [] { new BlockPotential(Flame, 1.0f) },
                        new BlockPotential(Smoke, 1.0f),
                        true,
                        false
                    ),
                    new Swapper(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor(
                "Spark",
                BlockTags.Gas,
                1.0f,
                new Color(255, 155, 0, 255, 0.4f),
                100.0f,
                1,
                new IBehavior[] {
                    new FireSpreader(1.0f,
                        FireColor,
                        9.0f,
                        new [] {
                            new BlockPotential(Spark, 0.08f),
                            new BlockPotential(Smoke, 0.4f)
                        },
                        new BlockPotential(Smoke, 0.1f),
                        false,
                        true
                    ),
                    new Swapper(
                        new [] { 5, 0, 3, 4, 1, 7, 2, 6 },
                        new [] { 4, 2, 4, 4, 4, 4, 4, 4 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor(
                "Lava",
                BlockTags.Liquid,
                1.4f,
                new Color(255, 80, 0, 255, 0.05f),
                100.0f,
                1,
                new IBehavior[]
                {
                    new Consumer(
                        new [] { Water },
                        HardenedLava,
                        Smoke,
                        0.1f
                    ),
                    new FireSpreader(0.0f,
                        FireColor,
                        0.0f,
                        new [] { new BlockPotential(Smoke, 0.0001f) },
                        new BlockPotential(Air, 1.0f),
                        true,
                        false),
                    new Swapper(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 4, 4, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor (
                "HardenedLava",
                BlockTags.Solid,
                1.39f,
                new Color(40, 40, 40, 255, 0.2f),
                0.0f,
                0,
                new IBehavior[]
                {
                    new Swapper(
                        new [] { 0, 0 },
                        new [] { 2, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        new[] { BlockTags.Solid, BlockTags.Vegetation }
                    )
                }
            ),
            new BlockDescriptor(
                "Plant",
                BlockTags.Vegetation,
                3.0f,
                new Color(80, 160, 80, 255, 0.4f),
                100.0f,
                0,
                new IBehavior[]
                {
                    new PlantGrower(0.0f,
                        1.0f,
                        2,
                        0.5f,
                        new[] { 0.1f, 0.1f },
                        new [] { 32, 8 },
                        new [] { 64, 16 },
                        1,
                        16,
                        new [] { Air },
                        PlantRoot),
                    PlantFireSpreader
                }
            ),
            new BlockDescriptor(
                "PlantRoot",
                BlockTags.Vegetation,
                3.0f,
                new Color(210, 180, 140, 255, 0.2f),
                100.0f,
                0,
                new IBehavior[]
                {
                    new PlantGrower(0.0f,
                        -1.0f,
                        3,
                        0.4f,
                        new[] { 0.2f, 0.2f, 0.2f },
                        new [] { 32, 16, 8 },
                        new [] { 48, 32, 16 },
                        1,
                        16,
                        new [] { Dirt, Sand },
                        Plant),
                    PlantFireSpreader
                }
            ),
            new BlockDescriptor(
                "Grass",
                BlockTags.Vegetation,
                3.0f,
                new Color(42, 76, 0, 255, 0.2f),
                100.0f,
                0,
                new IBehavior[]
                {
                    new PlantGrower(0.0f,
                        1.0f,
                        1,
                        0.0f,
                        new[] { 0.1f, 0.1f, 0.1f, 0.1f },
                        new [] { 1 },
                        new [] { 5 },
                        1,
                        16,
                        new [] { Air },
                        Border),
                    PlantFireSpreader
                }
            )
        };

        public static Color GetBlockColor(int type)
        {
            return type == UnassignedBlockType ? UnassignedBlockColor : BlockDescriptors[type].Color;
        }

        public static readonly string[] BlockNames = BlockDescriptors.Select(d => d.Name).ToArray();
    }
}
