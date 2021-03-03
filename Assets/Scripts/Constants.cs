using System;
using Unity.Collections;
using UnityEngine;

public static class Constants
{
    public enum Blocks
    {
        Air,
        Cloud,
        Oil,
        Water,
        Sand,
        Dirt,
        Stone,
        Metal,
        Border
    }

    public static int LiquidThreshold = (int) Blocks.Water;
    public static int SolidThreshold = (int)Blocks.Dirt;
    public static readonly int CooldownBlockValue = 1000;

    public static readonly NativeArray<Color32> BlockColors = new NativeArray<Color32>(Enum.GetNames(typeof(Blocks)).Length, Allocator.Persistent) {
        [(int)Blocks.Air] = new Color32(0, 0, 0, 0),
        [(int)Blocks.Cloud] = new Color32(193, 190, 186, 127),
        [(int)Blocks.Oil] = new Color32(51, 38, 0, 200),
        [(int)Blocks.Water] = new Color32(15, 94, 156, 127),
        [(int)Blocks.Sand] = new Color32(155, 134, 69, 255),
        [(int)Blocks.Dirt] = new Color32(124, 94, 66, 255),
        [(int)Blocks.Stone] = new Color32(149, 148, 139, 255),
        [(int)Blocks.Metal] = new Color32(75, 75, 75, 255),
        [(int)Blocks.Border] = new Color32(255, 0, 0, 255),
    };
}