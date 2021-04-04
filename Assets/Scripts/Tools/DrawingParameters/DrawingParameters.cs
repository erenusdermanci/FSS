using System;
using UnityEngine;
using Utils;

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

        [HideInInspector]
        public int radius;
    }
}