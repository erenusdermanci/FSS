using System;
using UnityEngine;

namespace DebugTools
{
    public class GlobalDebugConfig : MonoBehaviour
    {
        public GlobalConfigStruct globalConfig;
        public static GlobalConfigStruct StaticGlobalConfig;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticGlobalConfig = new GlobalConfigStruct(globalConfig);
        }

        private void Update()
        {
            if (StaticGlobalConfig.Equals(globalConfig))
                return;
            StaticGlobalConfig = new GlobalConfigStruct(globalConfig);

            UpdateEvent?.Invoke(this, null);
        }

        [Serializable]
        public struct GlobalConfigStruct
        {
            public bool disableSave;
            public bool disableLoad;
            public bool monothreadSimulate;
            public int overrideGridSize;
            public bool pauseSimulation;
            public bool stepByStep;
            public bool outlineChunks;
            public bool hideCleanChunkOutlines;
            public bool drawDirtyRects;
            public bool disableDirtySystem;
            public bool saveAsTestScene;

            public GlobalConfigStruct(GlobalConfigStruct other)
            {
                monothreadSimulate = other.monothreadSimulate;
                overrideGridSize = other.overrideGridSize;
                pauseSimulation = other.pauseSimulation;
                stepByStep = other.stepByStep;
                outlineChunks = other.outlineChunks;
                hideCleanChunkOutlines = other.hideCleanChunkOutlines;
                drawDirtyRects = other.drawDirtyRects;
                disableDirtySystem = other.disableDirtySystem;
                saveAsTestScene = other.saveAsTestScene;
                disableSave = other.disableSave;
                disableLoad = other.disableLoad;
            }

            public bool Equals(GlobalConfigStruct other)
            {
                if (overrideGridSize != other.overrideGridSize
                    || monothreadSimulate != other.monothreadSimulate
                    || pauseSimulation != other.pauseSimulation
                    || stepByStep != other.stepByStep
                    || outlineChunks != other.outlineChunks
                    || hideCleanChunkOutlines != other.hideCleanChunkOutlines
                    || drawDirtyRects != other.drawDirtyRects
                    || disableDirtySystem != other.disableDirtySystem
                    || saveAsTestScene != other.saveAsTestScene
                    || disableSave != other.disableSave
                    || disableLoad != other.disableLoad)
                    return false;

                return true;
            }
        }
    }
}
