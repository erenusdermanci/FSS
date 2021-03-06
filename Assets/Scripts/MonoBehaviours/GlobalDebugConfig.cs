using System;
using UnityEngine;

public class GlobalDebugConfig : MonoBehaviour
{
    public GlobalConfigStruct globalConfig;
    public static GlobalConfigStruct StaticGlobalConfig;

    // Currently unused
    public static event EventHandler UpdateEvent;

    void Awake()
    {
        StaticGlobalConfig = new GlobalConfigStruct(globalConfig);
    }

    // Update is called once per frame
    void Update()
    {
        if (StaticGlobalConfig.Equals(globalConfig))
            return;
        StaticGlobalConfig = new GlobalConfigStruct(globalConfig);

        UpdateEvent?.Invoke(this, null);
    }

    [Serializable]
    public struct GlobalConfigStruct
    {
        public bool DisablePersistence;
        public bool MonothreadSimulate;
        public int RestrictGridSize;
        public bool EnableSimulation;
        public bool PauseSimulation;
        public bool StepByStep;
        public bool OutlineChunks;
        public bool DisableDirtySystem;

        public GlobalConfigStruct(GlobalConfigStruct other)
        {
            MonothreadSimulate = other.MonothreadSimulate;
            RestrictGridSize = other.RestrictGridSize;
            EnableSimulation = other.EnableSimulation;
            PauseSimulation = other.PauseSimulation;
            StepByStep = other.StepByStep;
            OutlineChunks = other.OutlineChunks;
            DisableDirtySystem = other.DisableDirtySystem;
            DisablePersistence = other.DisablePersistence;
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
                || DisablePersistence != other.DisablePersistence)
                return false;

            return true;
        }
    }
}
