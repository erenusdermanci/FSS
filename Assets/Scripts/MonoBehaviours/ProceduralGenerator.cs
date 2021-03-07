using System;
using UnityEngine;
using Utils;

namespace MonoBehaviours
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

        private void Update()
        {
            if (StaticGenerationModel.Equals(GenerationModel))
                return;
            StaticGenerationModel = new TerrainGenerationModel(GenerationModel);

            IsEnabled = Enabled;

            UpdateEvent?.Invoke(this, null);
        }
    }
}
