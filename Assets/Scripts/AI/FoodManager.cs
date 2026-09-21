using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// Registro de la comida activa en la escena. 
    public class FoodManager : MonoBehaviour
    {
        public static FoodManager Instance { get; private set; }

        [SerializeField] private Food foodPrefab;
        [SerializeField] private int maxFood = 10; // tope de seguridad si spamean el botón

        private readonly List<Food> activeFood = new List<Food>();

        public int ActiveFoodCount => activeFood.Count;

        private void Awake() => Instance = this;

        /// Enganchar directo al OnClick() del botón "Dar de comer" en el Inspector.
        public void SpawnFood()
        {
            if (foodPrefab == null || ArenaBounds.Instance == null) return;
            if (activeFood.Count >= maxFood) return;

            Vector3 pos = ArenaBounds.Instance.RandomPointInside();
            Food food = Instantiate(foodPrefab, pos, Quaternion.identity, transform);
            activeFood.Add(food);
        }

        public void NotifyConsumed(Food food)
        {
            activeFood.Remove(food);
            Destroy(food.gameObject, 0.1f);
        }

        public Food GetNearestFood(Vector3 fromPosition, float maxRange)
        {
            Food nearest = null;
            float nearestSqrDist = maxRange * maxRange;

            for (int i = 0; i < activeFood.Count; i++)
            {
                Food food = activeFood[i];
                if (food == null || food.IsConsumed) continue;

                float sqrDist = (food.Position - fromPosition).sqrMagnitude;
                if (sqrDist <= nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = food;
                }
            }

            return nearest;
        }
    }
}
