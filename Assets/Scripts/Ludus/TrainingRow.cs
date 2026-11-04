using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Una opcion de entrenamiento: duracion, mejora que da y boton para empezar.
    public class TrainingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text detailLabel;
        [SerializeField] private Button startButton;

        public void Setup(int tierIndex, EconomyConfig.TrainingTier tier, Action<int> onStart)
        {
            titleLabel.text = $"{tier.minutes} minutos";
            detailLabel.text = $"+{tier.bonusHP:0} vida  ·  +{tier.bonusDamage:0.#} daño";

            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => onStart(tierIndex));
        }

        public void SetInteractable(bool value) => startButton.interactable = value;
    }
}
