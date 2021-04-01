using System;
using UnityEngine;

namespace Client.Camera
{
    public class FollowBehavior : MonoBehaviour
    {
        public Transform gameObjectToFollow;
        private UnityEngine.Camera _camera;

        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        public void Update()
        {
            var position = gameObjectToFollow.position;
            _camera.transform.position = new Vector3(position.x, position.y, -10);
        }
    }
}