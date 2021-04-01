using UnityEngine;

namespace Client.Camera
{
    public class FloatingBehavior : MonoBehaviour
    {
        private UnityEngine.Camera _camera;
        private bool _dragging;
        private Vector3 _dragStart;
        private Vector3 _dragDiff;

        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _dragStart = Vector3.zero;
            _dragDiff = Vector3.zero;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _dragging = false;
            }
        }

        private void LateUpdate()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (_dragging)
                    return;
                _dragging = true;
                _dragStart = _camera.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                if (!_dragging)
                    return;
                _dragging = false;
                _dragStart = Vector3.zero;
            }
            else if (Input.GetMouseButton(1))
            {
                if (!_dragging)
                    return;
                var cameraTransform = _camera.transform;
                _dragDiff = (_camera.ScreenToWorldPoint(Input.mousePosition)) - cameraTransform.position;
                cameraTransform.position = _dragStart - _dragDiff;
            }

        }
    }
}