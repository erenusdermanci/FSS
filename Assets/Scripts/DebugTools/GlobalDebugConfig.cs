using System;
using UnityEngine;

namespace DebugTools
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
            public bool disableSave;
            public bool disableLoad;
            public bool enableProceduralGeneration;
            public bool monoThreadSimulate;
            public bool pauseSimulation;
            public bool stepByStep;
            public bool outlineChunks;
            public bool hideCleanChunkOutlines;
            public bool drawDirtyRects;
            public bool disableDirtyChunks;
            public bool disableDirtyRects;
            public bool disableCollisions;
            public bool saveAsTestScene;

            public GlobalConfigStruct(GlobalConfigStruct other)
            {
                monoThreadSimulate = other.monoThreadSimulate;
                pauseSimulation = other.pauseSimulation;
                stepByStep = other.stepByStep;
                outlineChunks = other.outlineChunks;
                hideCleanChunkOutlines = other.hideCleanChunkOutlines;
                drawDirtyRects = other.drawDirtyRects;
                disableDirtyChunks = other.disableDirtyChunks;
                disableDirtyRects = other.disableDirtyRects;
                saveAsTestScene = other.saveAsTestScene;
                disableSave = other.disableSave;
                disableLoad = other.disableLoad;
                enableProceduralGeneration = other.enableProceduralGeneration;
                disableCollisions = other.disableCollisions;
            }

            public bool Equals(GlobalConfigStruct other)
            {
                if (monoThreadSimulate != other.monoThreadSimulate
                    || pauseSimulation != other.pauseSimulation
                    || stepByStep != other.stepByStep
                    || outlineChunks != other.outlineChunks
                    || hideCleanChunkOutlines != other.hideCleanChunkOutlines
                    || drawDirtyRects != other.drawDirtyRects
                    || disableDirtyChunks != other.disableDirtyChunks
                    || disableDirtyRects != other.disableDirtyRects
                    || saveAsTestScene != other.saveAsTestScene
                    || disableSave != other.disableSave
                    || disableLoad != other.disableLoad
                    || enableProceduralGeneration != other.enableProceduralGeneration
                    || disableCollisions != other.disableCollisions)
                    return false;

                return true;
            }
        }
    }
}
