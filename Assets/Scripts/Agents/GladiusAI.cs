using UnityEngine;

namespace GladiusAI
{
    public enum PlayerIntent { None, Attack, Defend, Surrender }

    /// <summary>
    /// Traduce los botones de la UI en una "consigna" que el árbol del
    /// gladiador del jugador puede llegar a seguir, con cierta probabilidad.
    /// No es control directo, es una sugerencia.
    /// </summary>
    public class PlayerIntentController : MonoBehaviour
    {
        [Range(0f, 1f)]
        [SerializeField] private float intentWeight = 0.75f; // 75% de probabilidad

        public PlayerIntent CurrentIntent { get; private set; } = PlayerIntent.None;

        public void RequestAttack() => CurrentIntent = PlayerIntent.Attack;
        public void RequestDefend() => CurrentIntent = PlayerIntent.Defend;
        public void RequestSurrender() => CurrentIntent = PlayerIntent.Surrender;

        /// <summary>Tira el dado según el peso configurado.</summary>
        public bool RollFor(PlayerIntent intent)
            => CurrentIntent == intent && Random.value <= intentWeight;

        public void ClearIntent() => CurrentIntent = PlayerIntent.None;
    }
}