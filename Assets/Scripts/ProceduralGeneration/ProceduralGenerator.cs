using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProceduralGeneration
{
    public class ProceduralGenerator : MonoBehaviour
    {
        [FormerlySerializedAs("GenerationModel")] public TerrainGenerationModel generationModel;
        public static TerrainGenerationModel StaticGenerationModel;

        public static event EventHandler UpdateEvent;

        private void Awake()
        {
            StaticGenerationModel = new TerrainGenerationModel(generationModel);
        }

        private void FixedUpdate()
        {
            if (StaticGenerationModel.Equals(generationModel))
                return;
            StaticGenerationModel = new TerrainGenerationModel(generationModel);

            UpdateEvent?.Invoke(this, null);
        }
    }
}
