using System.Collections.Generic;
using UnityEngine;

namespace OrbitFlock
{
    internal sealed class ActorPool
    {
        private readonly CombatActor prefab;
        private readonly Transform parent;
        private readonly Stack<CombatActor> available = new Stack<CombatActor>();

        public ActorPool(CombatActor prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public CombatActor Rent(CombatWorld world, Vector2 position, int health)
        {
            CombatActor actor = available.Count > 0 ? available.Pop() : Object.Instantiate(prefab, parent);
            actor.Rent(world, this, position, health);
            return actor;
        }

        public void Return(CombatActor actor)
        {
            actor.Sleep();
            available.Push(actor);
        }
    }
}
