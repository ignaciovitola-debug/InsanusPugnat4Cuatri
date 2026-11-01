using UnityEngine;
using TMPro;

namespace GladiusAI
{
    public class CombatResultDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultLabel;

        public void Configure(TMP_Text label) => resultLabel = label;

        private void OnEnable() => EventManager.Subscribe<CombatEndedEvent>(ShowResult);
        private void OnDisable() => EventManager.Unsubscribe<CombatEndedEvent>(ShowResult);

        private void ShowResult(CombatEndedEvent e)
        {
            if (resultLabel == null) return;

            resultLabel.text = e.result switch
            {
                CombatResult.Victory => e.reward > 0 ? $"¡VICTORIA!\n+{e.reward} denarios" : "¡VICTORIA!",
                CombatResult.Defeat => "DERROTA — Tu gladiador cayó en combate",
                CombatResult.Surrender => "Te rendiste",
                _ => resultLabel.text
            };
            resultLabel.gameObject.SetActive(true);
        }
    }
}
