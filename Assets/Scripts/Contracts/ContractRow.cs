using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Una fila del panel "Combates": dificultad, rivales y zona, premio y boton para entrar.
    public class ContractRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text detailLabel;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private Button fightButton;

        public void Setup(CombatContract contract, Action<CombatContract> onChoose)
        {
            string rivals = contract.enemyCount == 1 ? "1 rival" : $"{contract.enemyCount} rivales";
            titleLabel.text = contract.difficulty.DisplayName;
            detailLabel.text = $"{rivals}  ·  {contract.zoneName}";
            rewardLabel.text = DenariiText.Amount(contract.reward);

            fightButton.onClick.RemoveAllListeners();
            fightButton.onClick.AddListener(() => onChoose(contract));
        }
    }
}
