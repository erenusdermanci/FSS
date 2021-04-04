using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Utils.UnityHelpers
{
    public static class Helpers
    {
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