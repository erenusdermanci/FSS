using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalConfig : MonoBehaviour
{
    [Serializable]
    public struct GlobalConfigStruct
    {
        public bool MonothreadSimulate;
        public int RestrictGridSize;
        public bool EnableSimulation;
        public bool PauseSimulation;
        public bool StepByStep;
        public bool OutlineChunks;

        public GlobalConfigStruct(GlobalConfigStruct other)
        {
            MonothreadSimulate = other.MonothreadSimulate;
            RestrictGridSize = other.RestrictGridSize;
            EnableSimulation = other.EnableSimulation;
            PauseSimulation = other.PauseSimulation;
            StepByStep = other.StepByStep;
            OutlineChunks = other.OutlineChunks;
        }

        public bool Equals(GlobalConfigStruct other)
        {
            if (RestrictGridSize != other.RestrictGridSize
                || MonothreadSimulate != other.MonothreadSimulate
                || EnableSimulation != other.EnableSimulation
                || PauseSimulation != other.PauseSimulation
                || StepByStep != other.StepByStep
                || OutlineChunks != other.OutlineChunks)
                return false;

            return true;
        }
    }

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
}
