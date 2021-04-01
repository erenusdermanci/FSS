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

        private Camera _mainCamera;
        public static Vector3 MainCameraPosition = Vector3.zero;

        public Vector2i CameraFlooredPosition;
        [NonSerialized] public bool CameraHasMoved;
        private Vector2i? _oldCameraFlooredPosition;

        public static int UpdatedFlag;

        public event EventHandler GeneratedAreaSizeChanged;

        private void Awake()
        {
            _previousGeneratedAreaSize = generatedAreaSize;

            if (Camera.main != null)
            {
                _mainCamera = Camera.main;
                MainCameraPosition = _mainCamera.transform.position;
            }

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

            CameraHasMoved = UpdateCameraHasMoved();

            if (CameraHasMoved && _mainCamera != null)
            {
                MainCameraPosition = _mainCamera.transform.position;
            }
        }

        private static void DisableDirtyRectsChangedEvent(object sender, EventArgs e)
        {
            SimulationTask.ResetKnuthShuffle();
        }

        private bool UpdateCameraHasMoved()
        {
            if (_mainCamera == null)
                return false;
            var position = _mainCamera.transform.position;
            CameraFlooredPosition = new Vector2i((int) Mathf.Floor(position.x + 0.5f), (int) Mathf.Floor(position.y + 0.5f));
            if (_oldCameraFlooredPosition == CameraFlooredPosition)
                return false;
            _oldCameraFlooredPosition = CameraFlooredPosition;
            return true;
        }
    }
}
