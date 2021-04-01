using UnityEngine;
using Utils;

namespace Client.Camera
{
    public class ZoomBehavior : MonoBehaviour
    {
        public float minZoom;
        public float maxZoom;
        public float zoomAmount;
        public float zoomSpeed;

        private UnityEngine.Camera _camera;
        private float _targetZoom;
        private bool _zooming;

        public void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _targetZoom = _camera.orthographicSize;
        }

        public void Update()
        {
            var scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0)
            {
                _zooming = true;
                _targetZoom += scrollDelta < 0 ? zoomAmount : -zoomAmount;

                if (_targetZoom > maxZoom)
                    _targetZoom = maxZoom;
                else if (_targetZoom < minZoom)
                    _targetZoom = minZoom;
            }

            if (_zooming)
            {
                if (!_camera.orthographicSize.EqualsEpsilon(_targetZoom, 0.005f))
                {
                    _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetZoom, Time.deltaTime * zoomSpeed);
                }
                else
                {
                    _zooming = false;
                }
            }
        }
    }
}