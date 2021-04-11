using Chunks;
using UnityEditor;

namespace Entities.Editor
{
    [CustomEditor(typeof(Entity))]
    public class EntityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var entity = (Entity) target;

            entity.SetChunkLayerType((ChunkLayerType) EditorGUILayout.EnumPopup("Layer", entity.chunkLayerType));
        }
    }
}