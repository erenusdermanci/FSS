using System;
using Blocks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tools.BlockMapper.Editor
{
    [CustomEditor(typeof(BlockMapper))]
    public class BlockMapperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var blockMapper = (BlockMapper) target;

            blockMapper.selectedBlock = EditorGUILayout.Popup("Block", blockMapper.selectedBlock, BlockConstants.BlockNames);

            try
            {
                if (GUI.changed)
                {
                    EditorUtility.SetDirty(blockMapper);
                    EditorSceneManager.MarkSceneDirty(blockMapper.gameObject.scene);
                }
            }
            catch (Exception)
            {
                // When the scene is playing an exception is thrown because we cannot save the fields
                // I didn't find a way to check if the scene is playing
                // so silently catch it
            }
        }
    }
}