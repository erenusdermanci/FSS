using UnityEngine;

public static class BlockConstants
{
    public enum Blocks
    {
        Air,
        Cloud,
        Oil,
        Water,
        Sand,
        Wood,
        Dirt,
        Stone,
        Metal,
        Fire,
        Border
    }

    public static int LiquidThreshold = (int) Blocks.Water;
    public static int SolidThreshold = (int)Blocks.Metal;
    public static readonly int AlreadyUpdated = 1000;

    // don't forget to a color for each block
    public static readonly Color32[] BlockColors = {
        /* Air    */ new Color32(0, 0, 0, 0),
        /* Cloud  */ new Color32(193, 190, 186, 127),
        /* Oil    */ new Color32(51, 38, 0, 255),
        /* Water  */ new Color32(15, 94, 156, 255),
        /* Sand   */ new Color32(155, 134, 69, 255),
        /* Wood   */ new Color32(150, 90, 66, 255),
        /* Dirt   */ new Color32(124, 94, 66, 255),
        /* Stone  */ new Color32(149, 148, 139, 255),
        /* Metal  */ new Color32(75, 75, 75, 255),
        /* Fire  */ new Color32(240, 230, 100, 255),
        /* Border */ new Color32(255, 0, 0, 255),
    };

    public static readonly float[] BlockColorMaxShift =
    {
        /* Air    */ 0.0f,
        /* Cloud  */ 0.05f,
        /* Oil    */ 0.1f,
        /* Water  */ 0.025f,
        /* Sand   */ 0.05f,
        /* Wood   */ 0.05f,
        /* Dirt   */ 0.1f,
        /* Stone  */ 0.2f,
        /* Metal  */ 0.01f,
        /* Fire   */ 0.0f,
        /* Border */ 0.0f
    };

    // 0.0 to 1.0 (0 to 100% chance to burn)
    public static readonly float[] BlockFlammabilityChance =
    {
        /* Air    */ 0.0f,
        /* Cloud  */ 0.0f,
        /* Oil    */ 1.0f,
        /* Water  */ 0.0f,
        /* Sand   */ 0.0f,
        /* Wood   */ 0.1f,
        /* Dirt   */ 0.0f,
        /* Stone  */ 0.0f,
        /* Metal  */ 0.0f,
        /* Fire   */ 0.0f,
        /* Border */ 0.0f
    };
}