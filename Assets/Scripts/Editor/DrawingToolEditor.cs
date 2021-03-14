using System;
using System.Collections.Generic;
using System.Linq;
using Blocks;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(DrawingTool))] //1
    public class DrawingToolEditor : UnityEditor.Editor
    {
        private readonly string[] _blockNames = BlockLogic.BlockDescriptors.Select(d => d.Name).ToArray();
        private readonly string[] _stateNames = CreateStates().ToArray();

        public override void OnInspectorGUI() //2
        {
            base.OnInspectorGUI();

            var tool = (DrawingTool) target;

            tool.SelectedDrawBlock = EditorGUILayout.Popup("Block", tool.SelectedDrawBlock, _blockNames);
            tool.SelectedState = EditorGUILayout.Popup("State", tool.SelectedState, _stateNames);
        }

        private static IEnumerable<string> CreateStates()
        {
            yield return "No state";
            foreach (var state in Enum.GetNames(typeof(BlockLogic.States)))
                yield return state;
        }
    }
}
