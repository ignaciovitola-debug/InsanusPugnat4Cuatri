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

        private GladiatorNPC owner;

        /// <summary>El gladiador que recibe estas consignas (lo asigna GladiatorNPC.SetIntentController).</summary>
        public void SetOwner(GladiatorNPC gladiator) => owner = gladiator;

        /// <summary>Espera restante (0..1) antes de poder dar otra orden de Atacar/Defender.</summary>
        public float OrderCooldownRatio => owner != null ? owner.OrderCooldownRatio : 0f;

        public void RequestAttack() => CurrentIntent = PlayerIntent.Attack;
        public void RequestDefend() => CurrentIntent = PlayerIntent.Defend;
        public void RequestSurrender() => CurrentIntent = PlayerIntent.Surrender;

        /// <summary>Tira el dado según el peso configurado.</summary>
        public bool RollFor(PlayerIntent intent)
            => CurrentIntent == intent && Random.value <= intentWeight;

        public void ClearIntent() => CurrentIntent = PlayerIntent.None;
    }
}