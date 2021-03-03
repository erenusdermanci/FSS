using System;
using Unity.Collections;
using UnityEngine;

public static class Constants
{
    public enum Blocks
    {
        Air,
        Oil,
        Water,
        Sand,
        Stone,
        Metal,
        Border
    }

    public static int LiquidThreshold = (int) Blocks.Water;
    public static int SolidThreshold = (int)Blocks.Metal;
    public static readonly int CooldownBlockValue = 1000;

    public static readonly NativeArray<Color32> BlockColors = new NativeArray<Color32>(Enum.GetNames(typeof(Blocks)).Length, Allocator.Persistent) {
        [(int)Blocks.Air] = new Color32(0, 0, 0, 0),
        [(int)Blocks.Oil] = new Color32(51, 38, 0, 255),
        [(int)Blocks.Water] = new Color32(15, 94, 156, 255),
        [(int)Blocks.Sand] = new Color32(155, 134, 69, 255),
        [(int)Blocks.Stone] = new Color32(149, 148, 139, 255),
        [(int)Blocks.Metal] = new Color32(75, 75, 75, 255),
        [(int)Blocks.Border] = new Color32(255, 0, 0, 255),
    };
}