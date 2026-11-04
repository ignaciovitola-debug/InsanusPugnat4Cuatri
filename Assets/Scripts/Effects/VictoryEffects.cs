using UnityEngine;

namespace GladiusAI
{
    // Dispara el confeti cuando se gana un combate. Escucha CombatEndedEvent, no pregunta nada cada frame.
    public class VictoryEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem confettiPrefab;

        private void OnEnable() => EventManager.Subscribe<CombatEndedEvent>(OnCombatEnded);
        private void OnDisable() => EventManager.Unsubscribe<CombatEndedEvent>(OnCombatEnded);

        private void OnCombatEnded(CombatEndedEvent e)
        {
            if (e.result != CombatResult.Victory || confettiPrefab == null) return;

            // El prefab se destruye solo cuando termina (Stop Action = Destroy).
            Instantiate(confettiPrefab, transform.position, Quaternion.identity);
        }
    }
}
