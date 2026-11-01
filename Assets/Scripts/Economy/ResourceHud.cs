using TMPro;
using UnityEngine;

namespace GladiusAI
{
    // Muestra denarios y energia (con la cuenta regresiva hasta el proximo punto).
    // Se actualiza apenas cambia algo (EventManager) y dos veces por segundo para el reloj.
    public class ResourceHud : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;

        [SerializeField] private TMP_Text denariiLabel;
        [SerializeField] private TMP_Text energyLabel;

        private float nextRefresh;

        private void OnEnable()
        {
            EventManager.Subscribe<DenariiChangedEvent>(OnDenariiChanged);
            EventManager.Subscribe<EnergyChangedEvent>(OnEnergyChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<DenariiChangedEvent>(OnDenariiChanged);
            EventManager.Unsubscribe<EnergyChangedEvent>(OnEnergyChanged);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        private void OnDenariiChanged(DenariiChangedEvent e) => Refresh();
        private void OnEnergyChanged(EnergyChangedEvent e) => Refresh();

        private void Refresh()
        {
            if (denariiLabel != null)
                denariiLabel.text = $"{Denarii.Balance} denarios";

            if (energyLabel == null) return;

            int current = Energy.Current;
            energyLabel.text = current >= Energy.Max
                ? $"Energía {current}/{Energy.Max}"
                : $"Energía {current}/{Energy.Max}  ·  próxima en {Energy.TimeToNextText()}";
        }
    }
}
