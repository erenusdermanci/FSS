using UnityEditor;
using Utils.UnityHelpers;

namespace Tools.LevelDesigner.Editor
{
    [CustomEditor(typeof(LevelDesigner))]
    public class LevelDesignerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var levelDesigner = (LevelDesigner) target;

            Helpers.SetEditorDirty(levelDesigner);
        }
    }
}