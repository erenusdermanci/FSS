using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProceduralGeneration
{
    public class ProceduralGenerator : MonoBehaviour
    {
        public bool Enabled;
        public static bool IsEnabled;

        [FormerlySerializedAs("GenerationModel")] public TerrainGenerationModel generationModel;
        public static TerrainGenerationModel StaticGenerationModel;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticGenerationModel = new TerrainGenerationModel(generationModel);

            IsEnabled = Enabled;
        }

        private void FixedUpdate()
        {
            if (StaticGenerationModel.Equals(generationModel))
                return;
            StaticGenerationModel = new TerrainGenerationModel(generationModel);

            IsEnabled = Enabled;

            UpdateEvent?.Invoke(this, null);
        }
    }
}
