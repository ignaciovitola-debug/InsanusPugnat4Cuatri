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
            // Sube siempre vida y ataque; cuanto mas dura, mas chances de doble y de mejorar la guardia.
            detailLabel.text = $"+{tier.bonusHP:0} vida · +{tier.bonusDamage:0.#} ataque\n" +
                               $"Doble {tier.doubleChance:P0} · Guardia {tier.guardChance:P0}";

            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => onStart(tierIndex));
        }

        public void SetInteractable(bool value) => startButton.interactable = value;
    }
}
