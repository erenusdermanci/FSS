using System;
using Serialized;
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                // Save
                TerrainGenerationModelSerializer.SaveModel(StaticGenerationModel);
            }
            else if (Input.GetKeyDown(KeyCode.F6))
            {
                // Load
                generationModel = new TerrainGenerationModel(TerrainGenerationModelSerializer.LoadModel());
            }
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
