using System.Collections.Generic;
using Tiles;
using UnityEngine;

namespace Entities
{
    public class EntityManager : MonoBehaviour
    {
        public readonly Dictionary<long, Entity> Entities = new Dictionary<long, Entity>();

        private readonly List<Entity> _entitiesToRemoveFromMap = new List<Entity>();

        public WorldManager worldManager;

        private void FixedUpdate()
        {
            foreach (var entity in _entitiesToRemoveFromMap)
            {
                //RemoveEntityFromMap(entity);
            }
            _entitiesToRemoveFromMap.Clear();
        }

        public void QueueEntityRemoveFromMap(Entity entity)
        {
            _entitiesToRemoveFromMap.Add(entity);
        }

        public void BlitStaticEntities()
        {
            foreach (var entity in Entities.Values)
            {
                if (entity.dynamic)
                    continue;
                //BlitEntity(entity);
            }
        }
    }
}