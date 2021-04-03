using System;
using UnityEngine;

namespace Tools
{
    public class GlobalDebugConfig : MonoBehaviour
    {
        public GlobalConfigStruct globalConfig;
        public static GlobalConfigStruct StaticGlobalConfig;

        public static event EventHandler DisableDirtyRectsChanged;

        private void Awake()
        {
            StaticGlobalConfig = new GlobalConfigStruct(globalConfig);
        }

        private void Update()
        {
            var oldDisableDirtyRects = StaticGlobalConfig.disableDirtyRects;
            if (StaticGlobalConfig.Equals(globalConfig))
                return;
            StaticGlobalConfig = new GlobalConfigStruct(globalConfig);
            if (StaticGlobalConfig.disableDirtyRects != oldDisableDirtyRects)
            {
                DisableDirtyRectsChanged?.Invoke(this, null);
            }
        }

        [Serializable]
        public struct GlobalConfigStruct
        {
            public bool pauseSimulation;
            public bool stepByStep;
            public bool outlineChunks;
            public bool hideCleanChunkOutlines;
            public bool drawDirtyRects;
            public bool disableDirtyChunks;
            public bool disableDirtyRects;
            public bool disableCollisions;
            public bool disableDrawingTool;

            public GlobalConfigStruct(GlobalConfigStruct other)
            {
                pauseSimulation = other.pauseSimulation;
                stepByStep = other.stepByStep;
                outlineChunks = other.outlineChunks;
                hideCleanChunkOutlines = other.hideCleanChunkOutlines;
                drawDirtyRects = other.drawDirtyRects;
                disableDirtyChunks = other.disableDirtyChunks;
                disableDirtyRects = other.disableDirtyRects;
                disableCollisions = other.disableCollisions;
                disableDrawingTool = other.disableDrawingTool;
            }

            public bool Equals(GlobalConfigStruct other)
            {
                if (pauseSimulation != other.pauseSimulation
                    || stepByStep != other.stepByStep
                    || outlineChunks != other.outlineChunks
                    || hideCleanChunkOutlines != other.hideCleanChunkOutlines
                    || drawDirtyRects != other.drawDirtyRects
                    || disableDirtyChunks != other.disableDirtyChunks
                    || disableDirtyRects != other.disableDirtyRects
                    || disableCollisions != other.disableCollisions
                    || disableDrawingTool != other.disableDrawingTool)
                    return false;

                return true;
            }
        }
    }
}
