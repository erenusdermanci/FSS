using Blocks.Behaviors;
using Unity.Collections;
using UnityEngine;
using static Blocks.BlockMovementType;
using static Blocks.BlockTags;

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
        public static readonly Color32 FireColor = new Color32(240, 127, 19, 255);

        public const int Air = 0;
        public const int Border = 8;
        public const int Smoke = 10;

        public static readonly BlockDescriptor[] BlockDescriptors = {
            new BlockDescriptor(
                "Air",
                NonPhysical,
                0.0f,
                new Color32(0, 0, 0, 0),
                0.0f,
                0.0f,
                0.0f,
                new IBehavior[] {}
            ),
            new BlockDescriptor(
                "Cloud",
                NonPhysical,
                0.0f,
                new Color32(193, 190, 186, 127),
                0.05f,
                0.0f,
                0.0f,
                new IBehavior[] {}
            ),
            new BlockDescriptor (
                "Oil",
                Liquid,
                0.1f,
                new Color32(51, 38, 0, 255),
                0.1f,
                0.1f,
                100.0f,
                new IBehavior[]
                {
                    new FireSpread(0.01f,
                        Smoke,
                        0.01f,
                        Smoke,
                        0.5f),
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            ),
            new BlockDescriptor (
                "Water",
                Liquid,
                0.2f,
                new Color32(15, 94, 156, 255),
                0.025f,
                0.0f,
                0.0f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            ),
            new BlockDescriptor (
                "Sand",
                Solid,
                0.5f,
                new Color32(155, 134, 69, 255),
                0.05f,
                0.0f,
                0.0f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            ),
            new BlockDescriptor (
                "Dirt",
                Solid,
                0.6f,
                new Color32(124, 94, 66, 255),
                0.1f,
                0.0f,
                0.0f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 2, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            ),
            new BlockDescriptor (
                "Stone",
                Solid,
                1.0f,
                new Color32(149, 148, 139, 255),
                0.2f,
                0.0f,
                0.0f,
                new IBehavior[] { }
            ),
            new BlockDescriptor (
                "Metal",
                Solid,
                1.0f,
                new Color32(75, 75, 75, 255),
                0.01f,
                0.0f,
                0.0f,
                new IBehavior[] { }
            ),
            new BlockDescriptor (
                "Border",
                Solid,
                1000.0f,
                new Color32(255, 0, 0, 255),
                0.0f,
                0.0f,
                0.0f,
                new IBehavior[] { }
            ),
            new BlockDescriptor(
                "Gas",
                Gas,
                0.05f,
                new Color32(73, 185, 96, 255),
                0.1f,
                1.0f,
                100.0f,
                new IBehavior[]
                {
                    new FireSpread(100.0f,
                        Smoke,
                        0.0f,
                        Smoke,
                        0.25f
                        ),
                    new Swap(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1 ,1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            ),
            new BlockDescriptor(
                "Smoke",
                Gas,
                0.06f,
                new Color32(59, 68, 75, 75),
                0.2f,
                0.0f,
                0.0f,
                new IBehavior[]
                {
                    new Despawn(0.1f,
                        100.0f,
                        Air
                    ),
                    new Swap(
                        new [] { 5, 5, 6, 7, 3, 4},
                        new [] { 0, 0, 0, 2, 2, 2, 1 ,1 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid
                    )
                }
            )
        };
    }
}