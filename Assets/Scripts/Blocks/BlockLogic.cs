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

    public static class BlockLogic
    {
        public const int Air = 0;
        public const int Border = 8;

        public static readonly BlockDescriptor[] BlockDescriptors = {
            new BlockDescriptor( // AIR
                0,
                NonPhysical,
                new BlockTags[] {},
                0.0f,
                new Color32(0, 0, 0, 0),
                0.0f,
                new IBehavior[] {}
            ),
            new BlockDescriptor( // CLOUD
                1,
                NonPhysical,
                new BlockTags[] {},
                0.0f,
                new Color32(193, 190, 186, 127),
                0.05f,
                new IBehavior[] {}
            ),
            new BlockDescriptor ( // OIL
                2,
                Liquid,
                new[] { Liquid },
                0.1f,
                new Color32(51, 38, 0, 255),
                0.1f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 2, 1, 1, 2, 2, 0, 0 ,0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // WATER
                3,
                Liquid,
                new [] { Liquid, Conductive },
                0.2f,
                new Color32(15, 94, 156, 255),
                0.025f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2, 3, 4 },
                        new [] { 4, 1, 1, 4, 4, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // SAND
                4,
                Solid,
                new [] { Solid },
                0.5f,
                new Color32(155, 134, 69, 255),
                0.05f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0, 1, 2 },
                        new [] { 2, 1, 1, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // DIRT
                5,
                Solid,
                new [] { Solid },
                0.6f,
                new Color32(124, 94, 66, 255),
                0.1f,
                new IBehavior[]
                {
                    new Swap(
                        new [] { 0, 0 },
                        new [] { 2, 0, 0, 0, 0, 0, 0, 0 },
                        new[] { Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized, Randomized },
                        Solid // check physicalTag
                    )
                }
            ),
            new BlockDescriptor ( // STONE
                6,
                Solid,
                new [] { Solid },
                1.0f,
                new Color32(149, 148, 139, 255),
                0.2f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // METAL
                7,
                Solid,
                new [] { Solid },
                1.0f,
                new Color32(75, 75, 75, 255),
                0.01f,
                new IBehavior[] { }
            ),
            new BlockDescriptor ( // BORDER
                8,
                Solid,
                new BlockTags[] { }, // no behavior
                1000.0f,
                new Color32(255, 0, 0, 255),
                0.0f,
                new IBehavior[] { }
            )
        };
    }
}