﻿using System;
using UnityEngine;

namespace ProceduralGeneration
{
    public class ProceduralGenerator : MonoBehaviour
    {
        public bool Enabled;
        public static bool IsEnabled;

        public TerrainGenerationModel GenerationModel;
        public static TerrainGenerationModel StaticGenerationModel;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticGenerationModel = new TerrainGenerationModel(GenerationModel);

            IsEnabled = Enabled;
        }

        private void FixedUpdate()
        {
            if (StaticGenerationModel.Equals(GenerationModel))
                return;
            StaticGenerationModel = new TerrainGenerationModel(GenerationModel);

            IsEnabled = Enabled;

            UpdateEvent?.Invoke(this, null);
        }
    }
}
