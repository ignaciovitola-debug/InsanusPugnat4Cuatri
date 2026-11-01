using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace GladiusAI
{
    /// Controla la escena Ludus: muestra el roster de 8 gladiadores, la seleccion, y arranca la Arena 2 con el elegido.
    public class LudusController : MonoBehaviour
    {
        [SerializeField] private LudusGladiatorSlot[] slots;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private TMP_Text selectionLabel;
        [SerializeField] private string nextSceneName = "ArenaDeCombate2";

        [Tooltip("Aviso cuando no hay energia para entrar al combate.")]
        [SerializeField] private TMP_Text noticeLabel;

        [SerializeField] private GameObject shopPanel;

        [Header("Tutorial (primera visita)")]
        [SerializeField] private TutorialDialogue dialogue;
        [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];

        private int selectedIndex;
        private bool started;

        private void OnEnable() => EventManager.Subscribe<RosterChangedEvent>(OnRosterChanged);
        private void OnDisable() => EventManager.Unsubscribe<RosterChangedEvent>(OnRosterChanged);

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].GetComponent<GladiatorNPC>()?.SetCombatEnabled(false);
                slots[i].Configure(i, this);
            }

            started = true;
            RefreshSlots();
            SelectSlot(0);

            if (!GladiatorRoster.LudusTutorialSeen && dialogue != null && introDialogue.Length > 0)
            {
                GladiatorRoster.LudusTutorialSeen = true;
                ShowHelp();
            }
        }

        // Boton "?": vuelve a mostrar el tutorial cuando se quiera.
        public void ShowHelp()
        {
            if (dialogue != null && introDialogue.Length > 0)
                StartCoroutine(dialogue.Play(introDialogue));
        }

        private void OnRosterChanged(RosterChangedEvent e)
        {
            if (!started) return;
            RefreshSlots();
            SelectSlot(GladiatorRoster.IsUnlocked(selectedIndex) ? selectedIndex : 0);
        }

        // Redibuja cada puesto con lo que dice el roster: aspecto, candado y cartel con la vida.
        private void RefreshSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var entry = GladiatorRoster.Slots[i];
                bool unlocked = entry.unlocked;

                slots[i].GetComponent<GladiatorNPC>()?.ApplySkin(entry.skin);
                slots[i].SetLocked(!unlocked);

                if (i < slotLabels.Length && slotLabels[i] != null)
                    slotLabels[i].text = unlocked ? $"{entry.label}\nHP {entry.maxHP:F0}" : $"{entry.label}\n¿ ? ?";
            }
        }

        public void SelectSlot(int index)
        {
            if (!GladiatorRoster.IsUnlocked(index)) return;

            selectedIndex = index;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetHighlighted(i == selectedIndex);

            if (selectionLabel != null)
                selectionLabel.text = $"Elegido: {GladiatorRoster.Slots[selectedIndex].label}";
        }

        public void ShowShop() => shopPanel.SetActive(true);
        public void HideShop() => shopPanel.SetActive(false);

        public void GoToCombat()
        {
            if (!Energy.TrySpend(EconomyConfig.Instance.energyPerCombat))
            {
                if (noticeLabel != null)
                {
                    noticeLabel.text = $"Sin energía. Próxima en {Energy.TimeToNextText()}";
                    noticeLabel.gameObject.SetActive(true);
                }
                return;
            }

            GladiatorRoster.SelectedIndex = selectedIndex;

            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("No hay SceneFader activo (¿arrancaste el juego desde Splash?) -- cargando sin fundido.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
