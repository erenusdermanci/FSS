using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(DrawingTool))]
    public class DrawingToolEditor : UnityEditor.Editor
    {
        private readonly string[] _blockNames = BlockConstants.BlockDescriptors.Select(d => d.Name).ToArray();

        public override void OnInspectorGUI() //2
        {
            base.OnInspectorGUI();

            var tool = (DrawingTool) target;

            tool.SelectedDrawBlock = EditorGUILayout.Popup("Block", tool.SelectedDrawBlock, _blockNames);
            if (!CanBlockBurn(tool.SelectedDrawBlock) && tool.SelectedState == ConvertState(BlockStates.Burning))
                tool.SelectedState = 0;
            tool.SelectedState = EditorGUILayout.Popup("State", tool.SelectedState, CreateStates(tool.SelectedDrawBlock).ToArray());
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
            return BlockConstants.BlockDescriptors[block].CombustionProbability > 0.0f;
        }

        private static int ConvertState(BlockStates state)
        {
            return (int) state + 1;
        }
    }
}
