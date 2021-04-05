using System.Collections.Generic;
using UnityEngine;

namespace Client.Camera
{
    public class CameraController : MonoBehaviour
    {
        private List<GameObject> _cameras;
        private int _currentCameraIndex;

        private void Start()
        {
            _cameras = new List<GameObject>();
            foreach (Transform child in transform)
            {
                _cameras.Add(child.gameObject);
            }

            _currentCameraIndex = 0;

            for (var i = 1; i < _cameras.Count; i++)
            {
                _cameras[i].gameObject.SetActive(false);
            }

            if (_cameras.Count <= 0)
                return;
            _cameras[0].gameObject.SetActive(true);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                _currentCameraIndex++;
                if (_currentCameraIndex < _cameras.Count)
                {
                    _cameras[_currentCameraIndex - 1].gameObject.SetActive(false);
                    _cameras[_currentCameraIndex].gameObject.SetActive(true);
                }
                else
                {
                    _cameras[_currentCameraIndex - 1].gameObject.SetActive(false);
                    _currentCameraIndex = 0;
                    _cameras[_currentCameraIndex].gameObject.SetActive(true);
                }
            }
        }
    }
}