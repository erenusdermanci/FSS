using Chunks;
using UnityEngine;

namespace Entities
{
    public class EntitySnap : MonoBehaviour
    {
        private const float GridUnitSize = 1f / Chunk.Size;

        private void Update()
        {
            SnapToGrid();
        }

        private void SnapToGrid()
        {
            var position = transform.position;
            position = new Vector2(
                Mathf.Round(position.x / GridUnitSize) * GridUnitSize,
                Mathf.Round(position.y / GridUnitSize) * GridUnitSize);
            transform.position = position;
        }
    }
}