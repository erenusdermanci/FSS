using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using UnityEditor;
using Utils;

namespace Tools.DrawingParameters.Editor
{
    [CustomEditor(typeof(DrawingParameters))]
    public class DrawingParametersEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var parameters = (DrawingParameters) target;

            if (parameters.tool == DrawingToolType.Brush)
            {
                parameters.brush = (DrawingBrushType) EditorGUILayout.EnumPopup("Brush", parameters.brush);
                switch (parameters.brush)
                {
                    case DrawingBrushType.Box:
                        parameters.size = EditorGUILayout.IntSlider("Size", parameters.size, 1, 1024);
                        break;
                    case DrawingBrushType.Circle:
                        parameters.size = EditorGUILayout.IntSlider("Radius", parameters.size, 1, 1024);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            parameters.block = EditorGUILayout.Popup("Block", parameters.block, BlockConstants.BlockNames);
            if (!CanBlockBurn(parameters.block) && parameters.state == ConvertState(BlockStates.Burning))
                parameters.state = 0;
            parameters.state = EditorGUILayout.Popup("State", parameters.state, CreateStates(parameters.block).ToArray());

            Helpers.SetEditorDirty(parameters);
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