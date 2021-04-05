using System;
using UnityEngine;
using Utils.Drawing;

namespace Tools.DrawingParameters
{
    [Serializable]
    public class DrawingParameters : MonoBehaviour
    {
        [HideInInspector]
        public int block;

        [HideInInspector]
        public int state;

        public DrawingToolType tool;

        [HideInInspector]
        public DrawingBrushType brush;

        [HideInInspector]
        public int size;

        public void DrawBrush(float x, float y, float unit)
        {
            var brushSize = unit * size;
            var cx = x * unit;
            var cy = y * unit;
            if (size % 2 != 0)
            {
                cx += unit / 2f;
                cy += unit / 2f;
            }
            switch (brush)
            {
                case DrawingBrushType.Box:
                {
                    cx -= brushSize / 2f;
                    cy -= brushSize / 2f;
                    DebugDraw.Rectangle(cx, cy, brushSize, brushSize, Color.red);
                    break;
                }
                case DrawingBrushType.Circle:
                {
                    DebugDraw.Circle(cx, cy, brushSize, Color.red);
                    break;
                }
            }
        }
    }
}