using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    public class GladiatorPool
    {
        private readonly IGladiatorFactory factory;
        private readonly Queue<GladiatorNPC> available = new Queue<GladiatorNPC>();

        public GladiatorPool(IGladiatorFactory factory) => this.factory = factory;

        public GladiatorNPC Get(Vector3 position, Quaternion rotation)
        {
            GladiatorNPC instance = available.Count > 0 ? available.Dequeue() : factory.CreateEnemy(position, rotation);
            if (instance == null) return null;

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.SetDestroyOnDeath(false);
            instance.ResetForReuse();
            return instance;
        }

        public void Release(GladiatorNPC instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            available.Enqueue(instance);
        }
    }
}
