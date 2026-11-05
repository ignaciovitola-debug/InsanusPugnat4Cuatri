using System.Collections;
using TMPro;
using UnityEngine;

namespace GladiusAI
{
    // Botones de orden habilitados en un combate. Nada marcado = todos.
    [System.Flags]
    public enum OrderSet
    {
        Attack = 1 << 0,
        Defend = 1 << 1,
        Surrender = 1 << 2,
    }

    // Todo lo del tutorial dentro del combate: dialogos, carteles y botones resaltados.
    public class TutorialFlow
    {
        // Que bit de OrderSet corresponde a cada boton de orderButtons.
        private static readonly OrderSet[] OrderButtonFlags = { OrderSet.Attack, OrderSet.Defend, OrderSet.Surrender };

        private readonly TutorialDialogue dialogue;
        private readonly TMP_Text hintLabel;
        private readonly GameObject[] orderButtons;
        private readonly PlayerIntentController intentController;

        public TutorialFlow(TutorialDialogue dialogue, TMP_Text hintLabel, GameObject[] orderButtons, PlayerIntentController intentController)
        {
            this.dialogue = dialogue;
            this.hintLabel = hintLabel;
            this.orderButtons = orderButtons;
            this.intentController = intentController;
        }

        // Muestra solo los botones de orden habilitados (Nothing = todos).
        public void ShowAvailableOrders(OrderSet available)
        {
            for (int i = 0; i < orderButtons.Length && i < OrderButtonFlags.Length; i++)
            {
                if (orderButtons[i] != null)
                    orderButtons[i].SetActive(available == 0 || (available & OrderButtonFlags[i]) != 0);
            }
        }

        public void SetPromptHighlight(PlayerIntent intent)
        {
            if (dialogue != null)
                dialogue.SetPromptHighlight(intent);
        }

        public IEnumerator PlayDialogue(DialogueLine[] lines)
        {
            if (dialogue != null && lines != null && lines.Length > 0)
                yield return dialogue.Play(lines);
        }

        public void ShowHint(string text)
        {
            if (hintLabel == null || string.IsNullOrEmpty(text)) return;
            hintLabel.text = text;
            hintLabel.gameObject.SetActive(true);
        }

        public void HideHint()
        {
            if (hintLabel != null)
                hintLabel.gameObject.SetActive(false);
        }

        // Frena el combate hasta que el jugador toque el boton pedido, que queda resaltado.
        public IEnumerator WaitForRequiredIntent(PlayerIntent required, string hint)
        {
            ShowHint(hint);
            SetPromptHighlight(required);

            while (intentController == null || intentController.CurrentIntent != required)
                yield return null;

            // Atacar y Defender quedan pendientes como una orden normal; Rendirse se descarta para no cortar el combate.
            if (required == PlayerIntent.Surrender)
                intentController.ClearIntent();
            SetPromptHighlight(PlayerIntent.None);
            HideHint();
        }
    }
}
