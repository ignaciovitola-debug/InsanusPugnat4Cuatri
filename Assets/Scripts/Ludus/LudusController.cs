using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace GladiusAI
{
    /// Controla la escena Ludus: muestra el roster de 8 gladiadores, la seleccion, y arranca el combate elegido con el gladiador elegido.
    public class LudusController : MonoBehaviour
    {
        [SerializeField] private LudusGladiatorSlot[] slots;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private TMP_Text selectionLabel;
        [Tooltip("Aviso cuando no hay energia para entrar al combate.")]
        [SerializeField] private TMP_Text noticeLabel;

        [SerializeField] private GameObject shopPanel;

        [Header("Tutorial")]
        [SerializeField] private TutorialDialogue dialogue;
        [SerializeField] private DialogueLine[] introDialogue = new DialogueLine[0];
        [Tooltip("Capitulo agregado despues (combates y entrenamiento): lo ve quien ya habia visto la introduccion.")]
        [SerializeField] private DialogueLine[] updateDialogue = new DialogueLine[0];

        [Header("Entrenamiento")]
        [SerializeField] private TrainingPanel trainingPanel;

        [Header("Combates (contratos)")]
        [SerializeField] private GameObject combatsPanel;
        [SerializeField] private RectTransform contractContent;
        [Tooltip("Fila de molde: queda apagada y se copia una vez por contrato.")]
        [SerializeField] private ContractRow contractRowTemplate;

        // Sube cuando se agregan mensajes nuevos al tutorial.
        private const int TutorialVersion = 2;

        private int selectedIndex;
        private bool started;
        private float nextTick;

        public int SelectedIndex => selectedIndex;

        private void OnEnable() => EventManager.Subscribe<RosterChangedEvent>(OnRosterChanged);
        private void OnDisable() => EventManager.Unsubscribe<RosterChangedEvent>(OnRosterChanged);

        // El entrenamiento termina por reloj: cada medio segundo se revisa y se actualiza la cuenta regresiva.
        private void Update()
        {
            if (!started || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.5f;

            if (Training.IsActive && !Training.TryComplete())
                RefreshSlots();
        }

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].GetComponent<GladiatorNPC>()?.SetCombatEnabled(false);
                slots[i].Configure(i, this);
            }

            started = true;
            Training.TryComplete();
            RefreshSlots();
            SelectSlot(FirstFreeSlot());

            int seen = GladiatorRoster.LudusTutorialVersion;
            if (seen < TutorialVersion)
            {
                GladiatorRoster.LudusTutorialVersion = TutorialVersion;
                PlayTutorial(includeIntro: seen == 0);
            }
        }

        // Boton "?": vuelve a mostrar el tutorial completo cuando se quiera.
        public void ShowHelp() => PlayTutorial(includeIntro: true);

        private void PlayTutorial(bool includeIntro)
        {
            if (dialogue == null) return;

            var lines = new System.Collections.Generic.List<DialogueLine>();
            if (includeIntro) lines.AddRange(introDialogue);
            lines.AddRange(updateDialogue);
            if (lines.Count > 0)
                StartCoroutine(dialogue.Play(lines));
        }

        private void OnRosterChanged(RosterChangedEvent e)
        {
            if (!started) return;
            RefreshSlots();
            SelectSlot(IsSelectable(selectedIndex) ? selectedIndex : FirstFreeSlot());
        }

        private static bool IsSelectable(int index)
            => GladiatorRoster.IsUnlocked(index) && Training.Slot != index;

        private static int FirstFreeSlot()
        {
            for (int i = 0; i < GladiatorRoster.Slots.Count; i++)
                if (IsSelectable(i)) return i;
            return 0;
        }

        public void ShowTraining() => trainingPanel.Open(selectedIndex);
        public void HideTraining() => trainingPanel.Close();

        // Redibuja cada puesto con lo que dice el roster: aspecto, candado y cartel con la vida.
        private void RefreshSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var entry = GladiatorRoster.Slots[i];
                bool unlocked = entry.unlocked;
                bool training = unlocked && Training.Slot == i;

                slots[i].GetComponent<GladiatorNPC>()?.ApplySkin(entry.skin);
                slots[i].SetLocked(!unlocked);
                slots[i].SetBusy(training);

                if (i >= slotLabels.Length || slotLabels[i] == null) continue;

                string name = entry.level > 0 ? $"{entry.label} (Nv {entry.level})" : entry.label;
                if (!unlocked) slotLabels[i].text = $"{entry.label}\n¿ ? ?";
                else if (training) slotLabels[i].text = $"{name}\nEntrenando {Training.TimeText()}";
                else slotLabels[i].text = $"{name}\nHP {entry.maxHP:F0}";
            }
        }

        public void SelectSlot(int index)
        {
            if (!IsSelectable(index)) return;

            selectedIndex = index;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetHighlighted(i == selectedIndex);

            if (selectionLabel != null)
                selectionLabel.text = $"Elegido: {GladiatorRoster.Slots[selectedIndex].label}";
        }

        public void ShowShop() => shopPanel.SetActive(true);
        public void HideShop() => shopPanel.SetActive(false);

        // Cada vez que se abre el panel se ofrecen contratos nuevos (zona al azar por dificultad).
        public void ShowCombats()
        {
            RebuildContractRows();
            combatsPanel.SetActive(true);
        }

        public void HideCombats() => combatsPanel.SetActive(false);

        private void RebuildContractRows()
        {
            contractRowTemplate.gameObject.SetActive(false);
            for (int i = contractContent.childCount - 1; i >= 0; i--)
            {
                var child = contractContent.GetChild(i);
                if (child != contractRowTemplate.transform)
                    Destroy(child.gameObject);
            }

            foreach (var contract in ContractFactory.CreateAll())
            {
                var row = Instantiate(contractRowTemplate, contractContent);
                row.gameObject.SetActive(true);
                row.Setup(contract, StartContract);
            }
        }

        private void StartContract(CombatContract contract)
        {
            if (!Energy.TrySpend(EconomyConfig.Instance.energyPerCombat))
            {
                HideCombats();
                if (noticeLabel != null)
                {
                    noticeLabel.text = $"Sin energía. Próxima en {Energy.TimeToNextText()}";
                    noticeLabel.gameObject.SetActive(true);
                }
                return;
            }

            GladiatorRoster.SelectedIndex = selectedIndex;
            CombatSetup.Current = contract;
            LoadScene(contract.zoneScene);
        }

        private void LoadScene(string sceneName)
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(sceneName);
            }
            else
            {
                Debug.LogWarning("No hay SceneFader activo (¿arrancaste el juego desde Splash?) -- cargando sin fundido.");
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
