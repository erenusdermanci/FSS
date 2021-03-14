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
            public bool DisableSave;
            public bool DisableLoad;
            public bool MonothreadSimulate;
            public int RestrictGridSize;
            public bool EnableSimulation;
            public bool PauseSimulation;
            public bool StepByStep;
            public bool OutlineChunks;
            public bool DisableDirtySystem;
            public bool SaveAsTestScene;

            public GlobalConfigStruct(GlobalConfigStruct other)
            {
                MonothreadSimulate = other.MonothreadSimulate;
                RestrictGridSize = other.RestrictGridSize;
                EnableSimulation = other.EnableSimulation;
                PauseSimulation = other.PauseSimulation;
                StepByStep = other.StepByStep;
                OutlineChunks = other.OutlineChunks;
                DisableDirtySystem = other.DisableDirtySystem;
                SaveAsTestScene = other.SaveAsTestScene;
                DisableSave = other.DisableSave;
                DisableLoad = other.DisableLoad;
            }

            public bool Equals(GlobalConfigStruct other)
            {
                if (RestrictGridSize != other.RestrictGridSize
                    || MonothreadSimulate != other.MonothreadSimulate
                    || EnableSimulation != other.EnableSimulation
                    || PauseSimulation != other.PauseSimulation
                    || StepByStep != other.StepByStep
                    || OutlineChunks != other.OutlineChunks
                    || DisableDirtySystem != other.DisableDirtySystem
                    || SaveAsTestScene != other.SaveAsTestScene
                    || DisableSave != other.DisableSave
                    || DisableLoad != other.DisableLoad)
                    return false;

                return true;
            }
        }
    }
}
