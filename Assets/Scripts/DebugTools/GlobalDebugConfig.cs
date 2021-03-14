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
            public int restrictGridSize;
            public bool pauseSimulation;
            public bool stepByStep;
            public bool outlineChunks;
            public bool disableDirtySystem;
            public bool saveAsTestScene;

            public GlobalConfigStruct(GlobalConfigStruct other)
            {
                monothreadSimulate = other.monothreadSimulate;
                restrictGridSize = other.restrictGridSize;
                pauseSimulation = other.pauseSimulation;
                stepByStep = other.stepByStep;
                outlineChunks = other.outlineChunks;
                disableDirtySystem = other.disableDirtySystem;
                saveAsTestScene = other.saveAsTestScene;
                disableSave = other.disableSave;
                disableLoad = other.disableLoad;
            }

            public bool Equals(GlobalConfigStruct other)
            {
                if (restrictGridSize != other.restrictGridSize
                    || monothreadSimulate != other.monothreadSimulate
                    || pauseSimulation != other.pauseSimulation
                    || stepByStep != other.stepByStep
                    || outlineChunks != other.outlineChunks
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
