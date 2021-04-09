using System;
using Entities;
using UnityEngine;

namespace Tools.LevelDesigner
{
    public class EntitySnap : MonoBehaviour
    {
        public Vector2 oldPosition;

        public Entity entity;

        public void Start()
        {
            oldPosition = transform.position;
        }

        public void Update()
        {
            if (!transform.position.Equals(oldPosition))
            {
                oldPosition = transform.position;
            }
        }

        private void Snap()
        {

        }
    }
}