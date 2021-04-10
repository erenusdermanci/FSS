using System;
using System.IO;
using Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils.UnityHelpers
{
    public static class Helpers
    {
        public static string InitialLoadPath()
        {
            var loadPath = $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}";
            return GlobalConfig.StaticGlobalConfig.initialLoadSceneOverride.Length != 0
                ? $"{loadPath}{GlobalConfig.StaticGlobalConfig.initialLoadSceneOverride}"
                : $"{SavePath()}";
        }

        public static string SavePath()
        {
            var savePath = $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}";
            return GlobalConfig.StaticGlobalConfig.saveSceneOverride.Length != 0
                ? $"{savePath}{GlobalConfig.StaticGlobalConfig.saveSceneOverride}"
                : $"{savePath}{SceneManager.GetActiveScene().name}";
        }

        public static void RemoveFilesInDirectory(string directoryPath)
        {
            var di = new DirectoryInfo(directoryPath);

            foreach (var file in di.EnumerateFiles())
            {
                file.Delete();
            }
        }

        public static float GetRandomShiftAmount(float baseAmount)
        {
            return baseAmount * ((float) StaticRandom.Get().NextDouble() - 0.5f) * 2.0f;
        }

        public static byte ShiftColorComponent(byte component, float amount)
        {
            return (byte)Mathf.Clamp(component + component * amount, 0.0f, 255.0f);
        }

        public static void SetEditorDirty(MonoBehaviour obj)
        {
            try
            {
                if (!GUI.changed)
                    return;
                EditorUtility.SetDirty(obj);
                EditorSceneManager.MarkSceneDirty(obj.gameObject.scene);
            }
            catch (Exception)
            {
                // When the scene is playing, an exception is thrown because we cannot save the fields
                // so silently catch it
            }
        }
    }
}
