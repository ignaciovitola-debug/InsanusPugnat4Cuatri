using UnityEngine;
using TMPro;

namespace GladiusAI
{
    /// <summary>
    /// Muestra el resultado del combate (Victoria/Derrota/Rendición) y registra
    /// cada muerte — sin que CombatStarter tenga que conocer nada de UI. Se
    /// entera de todo vía EventManager (Observer / Event Manager, Modelos y
    /// Algoritmos Clase 3): quien publica el evento no sabe que esto existe.
    /// </summary>
    public class CombatResultDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultLabel;

        /// <summary>Usado por la herramienta de Editor al conectar este componente — asigna la referencia una sola vez.</summary>
        public void Configure(TMP_Text label) => resultLabel = label;

        private void OnEnable()
        {
            EventManager.Subscribe<CombatEndedEvent>(ShowResult);
            EventManager.Subscribe<GladiatorDiedEvent>(LogDeath);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<CombatEndedEvent>(ShowResult);
            EventManager.Unsubscribe<GladiatorDiedEvent>(LogDeath);
        }

        private void ShowResult(CombatEndedEvent e)
        {
            if (resultLabel == null) return;

            resultLabel.text = e.result switch
            {
                CombatResult.Victory => "¡VICTORIA!",
                CombatResult.Defeat => "DERROTA — Tu gladiador cayó en combate",
                CombatResult.Surrender => "Te rendiste",
                _ => resultLabel.text
            };
            resultLabel.gameObject.SetActive(true);
        }

        private void LogDeath(GladiatorDiedEvent e) => Debug.Log($"[EventManager] Murió: {e.gladiatorName}");
    }
}
