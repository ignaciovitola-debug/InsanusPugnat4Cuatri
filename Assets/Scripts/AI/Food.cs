using UnityEngine;

namespace GladiusAI
{
    /// <summary>Un punto de comida individual. Los boids la consumen acercándose con Arrive.</summary>
    public class Food : MonoBehaviour
    {
        public bool IsConsumed { get; private set; }
        public Vector3 Position => transform.position;

        public void Consume()
        {
            if (IsConsumed) return;
            IsConsumed = true;
            FoodManager.Instance?.NotifyConsumed(this);
            gameObject.SetActive(false);
        }
    }
}
