using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace GladiusAI
{
    /// <summary>Controla la escena Ludus: muestra el roster de 8 gladiadores, la seleccion, y arranca la Arena 2 con el elegido.</summary>
    public class LudusController : MonoBehaviour
    {
        [SerializeField] private LudusGladiatorSlot[] slots;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private TMP_Text selectionLabel;
        [SerializeField] private string nextSceneName = "ArenaDeCombate2";

        private int selectedIndex;

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var npc = slots[i].GetComponent<GladiatorNPC>();
                npc?.SetCombatEnabled(false);

                slots[i].Configure(i, this);

                var entry = GladiatorRoster.Slots[i];
                if (i < slotLabels.Length && slotLabels[i] != null)
                    slotLabels[i].text = $"{entry.label}\nHP {entry.maxHP:F0}";
            }

            SelectSlot(0);
        }

        public void SelectSlot(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetHighlighted(i == selectedIndex);

            if (selectionLabel != null)
                selectionLabel.text = $"Elegido: {GladiatorRoster.Slots[selectedIndex].label}";
        }

        public void GoToCombat()
        {
            GladiatorRoster.SelectedIndex = selectedIndex;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
