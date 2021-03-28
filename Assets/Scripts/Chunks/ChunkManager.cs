using System;
using Chunks.Server;
using DebugTools;
using UnityEngine;
using Utils;

namespace Chunks
{
    public class ChunkManager : MonoBehaviour
    {
        public int generatedAreaSize = 10;
        private int _previousGeneratedAreaSize;

        public int cleanAreaSizeOffset = 2;
        public Transform playerTransform;

        public static Vector2 PlayerPosition;

        public Vector2i PlayerFlooredPosition;
        [NonSerialized] public bool PlayerHasMoved;
        private Vector2i? _oldPlayerFlooredPosition;

        public static int UpdatedFlag;

        public event EventHandler GeneratedAreaSizeChanged;

        private void Awake()
        {
            _previousGeneratedAreaSize = generatedAreaSize;

            PlayerPosition = playerTransform.position;

            GlobalDebugConfig.DisableDirtyRectsChanged += DisableDirtyRectsChangedEvent;

            UpdatedFlag = 1;
        }

        private void Update()
        {
            if (generatedAreaSize != _previousGeneratedAreaSize)
            {
                _previousGeneratedAreaSize = generatedAreaSize;
                GeneratedAreaSizeChanged?.Invoke(this, null);
            }
        }

        private void FixedUpdate()
        {
            UpdatedFlag++;

            PlayerHasMoved = UpdatePlayerHasMoved();

            if (PlayerHasMoved)
            {
                PlayerPosition = playerTransform.position;
            }
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        private bool UpdatePlayerHasMoved()
        {
            var position = playerTransform.position;
            PlayerFlooredPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldPlayerFlooredPosition == PlayerFlooredPosition)
                return false;
            _oldPlayerFlooredPosition = PlayerFlooredPosition;
            return true;
        }
    }
}
