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
                    new Despawn(0.2f,
                        10.0f,
                        0.2f,
                        Smoke
                    ),
                    new FireSpread(0.0f,
                        FireColor,
                        0.0f,
                        new int[] {},
                        new float[] {},
                        Air,
                        0.0f,
                        false,
                        false
                    ),
                    new Swap(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 1, 1, 1, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                    new FireSpread(0.1f,
                        FireColor,
                        0.01f,
                        new [] { Flame },
                        new [] { 1.0f },
                        Smoke,
                        0.5f,
                        true,
                        false),
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Farthest, Farthest, Randomized, Randomized, Randomized },
                        BlockTags.Solid
                    )
                }
            ),
            new BlockDescriptor (
                "Water",
                BlockTags.Liquid,
                1.2f,
                new Color(15, 94, 156, 255, 0.025f),
                0.0f,
                0,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Farthest, Farthest, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 2, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
                    )
                }
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
                new Color(255, 0, 0, 255, 0.0f),
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
                    new FireSpread(1.0f,
                        FireColor,
                        10.0f,
                        new [] { Smoke },
                            new [] { 0.0f },
                        Smoke,
                        0.25f,
                        false,
                        false
                    ),
                    new Swap(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                    new Despawn(0.1f,
                        100.0f,
                        1.0f,
                        Air
                    ),
                    new Swap(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1, 1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                    new FireSpread(1.0f,
                        FireColor,
                        5.0f,
                        new [] { Spark, Flame },
                            new [] { 0.12f, 1.0f },
                        Coal,
                        0.1f,
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
                    new FireSpread(0.2f,
                        FireColor,
                        0.01f,
                        new [] { Flame },
                            new [] { 1.0f },
                        Smoke,
                        1.0f,
                        true,
                        false
                    ),
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
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
                    new FireSpread(1.0f,
                        FireColor,
                        9.0f,
                        new [] { Spark, Smoke },
                        new [] { 0.08f, 0.4f },
                        Smoke,
                        0.1f,
                        false,
                        true
                    ),
                    new Swap(
                        new [] { 5, 0, 3, 4, 1, 7, 2, 6 },
                        new [] { 4, 2, 4, 4, 4, 4, 4, 4 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        BlockTags.Solid
                    )
                }
            )
        };

        public static readonly string[] BlockNames = BlockDescriptors.Select(d => d.Name).ToArray();
    }
}
