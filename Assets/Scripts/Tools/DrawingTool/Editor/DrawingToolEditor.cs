using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Utils;

namespace Tools.DrawingTool.Editor
{
    [CustomEditor(typeof(DrawingTool))]
    public class DrawingToolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var tool = (DrawingTool) target;

            tool.selectedDrawBlock = EditorGUILayout.Popup("Block", tool.selectedDrawBlock, BlockConstants.BlockNames);
            if (!CanBlockBurn(tool.selectedDrawBlock) && tool.selectedState == ConvertState(BlockStates.Burning))
                tool.selectedState = 0;
            tool.selectedState = EditorGUILayout.Popup("State", tool.selectedState, CreateStates(tool.selectedDrawBlock).ToArray());

            switch (tool.selectedDrawingBrush)
            {
                case DrawingBrushType.Box:
                    tool.boxSize = EditorGUILayout.IntSlider("Size", tool.boxSize, 0, 1024);
                    break;
                case DrawingBrushType.Circle:
                    tool.circleRadius = EditorGUILayout.IntSlider("Radius", tool.circleRadius, 0, 1024);
                    break;
            }

            try
            {
                if (GUI.changed)
                {
                    EditorUtility.SetDirty(tool);
                    EditorSceneManager.MarkSceneDirty(tool.gameObject.scene);
                }
            }
            catch (Exception)
            {
                // When the scene is playing an exception is thrown because we cannot save the fields
                // I didn't find a way to check if the scene is playing
                // so silently catch it
            }
        }

        private static IEnumerable<string> CreateStates(int selectedDrawBlock)
        {
            yield return "No state";
            foreach (var state in Enum.GetValues(typeof(BlockStates)))
            {
                switch (state)
                {
                    case BlockStates.Burning:
                        if (CanBlockBurn(selectedDrawBlock))
                            yield return state.ToString();
                        break;
                    default:
                        yield return state.ToString();
                        break;
                }
            }
        }

        private static bool CanBlockBurn(int block)
        {
            return BlockConstants.BlockDescriptors[block].FireSpreader != null
                && BlockConstants.BlockDescriptors[block].FireSpreader.CombustionProbability > 0.0f;
        }

        private static int ConvertState(BlockStates state)
        {
            return (int) state + 1;
        }
    }
}
