using System;
using Chunks;
using UnityEngine;

namespace Entities
{
    public class EntitySnap : MonoBehaviour
    {
        private const float GridUnitSize = 1f / Chunk.Size;

        private void Awake()
        {
            SnapToGrid();
        }

        private void FixedUpdate()
        {
            SnapToGrid();
        }

        private void SnapToGrid()
        {
            var position = transform.position;
            position.Set(
                Mathf.Round(position.x / GridUnitSize) * GridUnitSize,
                Mathf.Round(position.y / GridUnitSize) * GridUnitSize,
                0.0f
            );
            transform.position = position;
        }
    }
}