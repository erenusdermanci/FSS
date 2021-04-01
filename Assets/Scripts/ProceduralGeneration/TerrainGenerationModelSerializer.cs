using System;
using System.IO;
using Serialized;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralGeneration
{
    public static class TerrainGenerationModelSerializer
    {
        private static readonly string TgmSavePath = $"{Application.dataPath}\\..\\TerrainGenerationModel\\{SceneManager.GetActiveScene().name}";
        private static readonly string TgmSaveFullPath = $"{TgmSavePath}\\terrain_model.json";

        public static bool SaveModel(TerrainGenerationModel tgm)
        {
            try
            {
                if (!Directory.Exists(TgmSavePath))
                {
                    Directory.CreateDirectory(TgmSavePath);
                }

                var serialized = JsonUtility.ToJson(tgm, true);
                File.WriteAllText(TgmSaveFullPath, serialized);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }

            return true;
        }

        public static TerrainGenerationModel LoadModel()
        {
            var deserialized = File.ReadAllText(TgmSaveFullPath);
            return JsonUtility.FromJson<TerrainGenerationModel>(deserialized);
        }
    }
}
