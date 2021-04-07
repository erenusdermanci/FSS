using System;
using UnityEngine;

namespace Tools
{
    public class GlobalConfig : MonoBehaviour
    {
        public GlobalConfigFields globalConfig;
        public static GlobalConfigFields StaticGlobalConfig;

        public static event EventHandler DisableDirtyRectsChanged;

        private void Awake()
        {
            StaticGlobalConfig = new GlobalConfigFields(globalConfig);
        }

        private void Update()
        {
            var oldDisableDirtyRects = StaticGlobalConfig.disableDirtyRects;
            if (StaticGlobalConfig.Equals(globalConfig))
                return;
            StaticGlobalConfig = new GlobalConfigFields(globalConfig);
            if (StaticGlobalConfig.disableDirtyRects != oldDisableDirtyRects)
            {
                DisableDirtyRectsChanged?.Invoke(this, null);
            }
        }

        [Serializable]
        public class GlobalConfigFields
        {
            public string saveSceneOverride;
            public bool pauseSimulation;
            public bool stepByStep;
            public bool outlineChunks;
            public bool outlineTiles;
            public bool hideCleanChunkOutlines;
            public bool drawDirtyRects;
            public bool disableDirtyChunks;
            public bool disableDirtyRects;
            public bool disableCollisions;
            public bool disableDrawingTool;

            public GlobalConfigFields(GlobalConfigFields other)
            {
                saveSceneOverride = other.saveSceneOverride;
                pauseSimulation = other.pauseSimulation;
                stepByStep = other.stepByStep;
                outlineChunks = other.outlineChunks;
                outlineTiles = other.outlineTiles;
                hideCleanChunkOutlines = other.hideCleanChunkOutlines;
                drawDirtyRects = other.drawDirtyRects;
                disableDirtyChunks = other.disableDirtyChunks;
                disableDirtyRects = other.disableDirtyRects;
                disableCollisions = other.disableCollisions;
                disableDrawingTool = other.disableDrawingTool;
            }

            public bool Equals(GlobalConfigFields other)
            {
                if (saveSceneOverride != other.saveSceneOverride
                    || pauseSimulation != other.pauseSimulation
                    || stepByStep != other.stepByStep
                    || outlineChunks != other.outlineChunks
                    || outlineTiles != other.outlineTiles
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
