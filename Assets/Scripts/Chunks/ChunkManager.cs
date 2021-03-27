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
        public bool playerHasMoved;

        public static Vector2 PlayerPosition;

        public Vector2i playerFlooredPosition;
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

            playerHasMoved = PlayerHasMoved();

            if (playerHasMoved)
            {
                PlayerPosition = playerTransform.position;
            }
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        private bool PlayerHasMoved()
        {
            var position = playerTransform.position;
            playerFlooredPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldPlayerFlooredPosition == playerFlooredPosition)
                return false;
            _oldPlayerFlooredPosition = playerFlooredPosition;
            return true;
        }
    }
}
