using System;
using UnityEngine;

namespace ProceduralGeneration
{
    public class ProceduralGenerator : MonoBehaviour
    {
        public TerrainGenerationModel generationModel;
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
