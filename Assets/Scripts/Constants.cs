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
    public static int SolidThreshold = (int)Blocks.Metal;
    public static readonly int AlreadyUpdated = 1000;

    // don't forget to a color for each block
    public static readonly Color32[] BlockColors = {
        new Color32(0, 0, 0, 0),
        new Color32(193, 190, 186, 127),
        new Color32(51, 38, 0, 255),
        new Color32(15, 94, 156, 255),
        new Color32(155, 134, 69, 255),
        new Color32(124, 94, 66, 255),
        new Color32(149, 148, 139, 255),
        new Color32(75, 75, 75, 255),
        new Color32(255, 0, 0, 255),
    };
}