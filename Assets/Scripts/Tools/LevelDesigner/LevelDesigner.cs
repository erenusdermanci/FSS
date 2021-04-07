using System.Collections.Generic;
using Entities;
using UnityEngine;
using Utils;

namespace Tools.LevelDesigner
{
    public class LevelDesigner : MonoBehaviour
    {
        private List<Entity> _entities;

        public void EntityAwake(Entity entity)
        {
            _entities.Add(entity);

            // assign this entity Unique Id that will be transmitted to blocks when they are put in the grid
            entity.id = UniqueIdGenerator.Next();

            // add temporary collider on the entity, for mouse selection
            var tmpCollider = entity.gameObject.AddComponent<BoxCollider2D>();
            tmpCollider.tag = "LevelDesigner";
            tmpCollider.enabled = true;

            // move the entity at the camera position
            var cameraPosition = Camera.main.transform.position;
            entity.transform.position = new Vector2(cameraPosition.x, cameraPosition.y);
        }
    }
}