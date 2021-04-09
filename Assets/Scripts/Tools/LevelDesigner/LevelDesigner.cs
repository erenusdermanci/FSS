using System;
using System.Collections.Generic;
using Entities;
using UnityEngine;
using Utils;

namespace Tools.LevelDesigner
{
    public class LevelDesigner : MonoBehaviour
    {
        private Dictionary<long, Entity> _entities = new Dictionary<long, Entity>();

        public void EntityAwake(Entity entity)
        {
            // assign this entity Unique Id that will be transmitted to blocks when they are put in the grid
            entity.id = UniqueIdGenerator.Next();

            _entities.Add(entity.id, entity);

            // add temporary collider on the entity, for mouse selection
            var tmpCollider = entity.gameObject.AddComponent<BoxCollider2D>();
            tmpCollider.tag = "LevelDesigner";
            tmpCollider.enabled = true;

            // move the entity at the camera position
            var cameraPosition = Camera.main.transform.position;
            entity.transform.position = new Vector2(cameraPosition.x, cameraPosition.y);

            // add the block grid snapping script
            var snap = entity.gameObject.AddComponent<EntitySnap>();
            snap.entity = entity;
            snap.enabled = true;
        }

        private void EntityDestroyed(Entity childEntity)
        {
            _entities.Remove(childEntity.id);
        }

        private void OnDestroy()
        {
        }
    }
}