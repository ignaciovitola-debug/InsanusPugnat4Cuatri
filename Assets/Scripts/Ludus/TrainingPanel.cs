using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Panel de entrenamiento de la Ludus: se elige cuanto entrenar al gladiador seleccionado, y mientras
    // entrena se ve la cuenta regresiva y se puede apurar con denarios.
    public class TrainingPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private RectTransform content;
        [Tooltip("Fila de molde: queda apagada y se copia una vez por duracion.")]
        [SerializeField] private TrainingRow rowTemplate;
        [SerializeField] private Button rushButton;
        [SerializeField] private TMP_Text rushLabel;

        private readonly List<TrainingRow> rows = new List<TrainingRow>();
        private int slot;
        private float nextRefresh;

        private void Start()
        {
            rowTemplate.gameObject.SetActive(false);

            var tiers = EconomyConfig.Instance.trainingTiers;
            for (int i = 0; i < tiers.Length; i++)
            {
                var row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.Setup(i, tiers[i], StartTraining);
                rows.Add(row);
            }

            rushButton.onClick.AddListener(Rush);
        }

        public void Open(int slotIndex)
        {
            slot = slotIndex;
            messageLabel.text = string.Empty;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        private void StartTraining(int tierIndex)
        {
            messageLabel.text = Training.TryStart(slot, tierIndex, out string reason) ? string.Empty : reason;
            Refresh();
        }

        private void Rush()
        {
            if (!Training.TryRush(out string reason))
                messageLabel.text = reason;
            Refresh();
        }

        private void Refresh()
        {
            bool training = Training.IsActive;
            var roster = GladiatorRoster.Slots;

            if (training)
            {
                var entry = roster[Training.Slot];
                statusLabel.text = $"{entry.label} está entrenando: {Training.TimeText()}";
            }
            else
            {
                var entry = roster[Mathf.Clamp(slot, 0, roster.Count - 1)];
                statusLabel.text = entry.level > 0 ? $"Entrenar a {entry.label} (Nv {entry.level})" : $"Entrenar a {entry.label}";
            }

            foreach (var row in rows)
                row.SetInteractable(!training);

            rushButton.gameObject.SetActive(training);
            if (training)
                rushLabel.text = $"Apurar ({Training.RushCost} denarios)";
        }
    }
}
