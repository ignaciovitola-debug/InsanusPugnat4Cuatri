using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GladiusAI
{
    // Cuadro de diálogo del instructor: retrato + texto que aparece palabra por palabra.
    // Pausa el juego mientras está abierto. Se usa con: yield return dialogue.Play(lineas);
    public class TutorialDialogue : MonoBehaviour, IPointerClickHandler
    {
        [Header("Partes del cuadro")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text lineText;
        [Tooltip("Aparece cuando ya se puede pasar al siguiente mensaje.")]
        [SerializeField] private GameObject continueHint;
        [Tooltip("Un retrato por cara, en el orden de PortraitExpression.")]
        [SerializeField] private Sprite[] portraits = new Sprite[0];
        [Tooltip("Caras que miran a la izquierda: se espejan.")]
        [SerializeField] private PortraitExpression[] mirroredExpressions = { PortraitExpression.Smile, PortraitExpression.Special };

        [Header("Tiempos")]
        [Tooltip("Segundos que se ve la cara antes de hablar.")]
        [SerializeField] private float openingPause = 0.8f;
        [Tooltip("Segundos entre una palabra y la siguiente.")]
        [SerializeField] private float secondsPerWord = 0.12f;
        [Tooltip("Segundos que tarda cada palabra en aparecer.")]
        [SerializeField] private float wordFadeSeconds = 0.35f;
        [Tooltip("Segundos de lectura antes de permitir seguir.")]
        [SerializeField] private float readingPause = 2f;
        [Tooltip("Segundos con el cuadro cerrado antes de volver a abrirse (Appear Again).")]
        [SerializeField] private float reappearGap = 0.5f;

        [Header("Resaltado de botones de orden")]
        [Tooltip("En este orden: Atacá, Defiéndete, Ríndete.")]
        [SerializeField] private RectTransform[] orderButtons = new RectTransform[0];
        [Tooltip("Altura del texto mientras se resaltan botones.")]
        [SerializeField] private float textBottomWhileHighlighting = 140f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.3f);

        // Estado del mensaje actual
        private bool tapped;
        private int[] wordOfCharacter = new int[0]; // para cada letra, a qué palabra pertenece (se reutiliza)

        // Estado del resaltado
        private DialogueHighlight currentHighlight = DialogueHighlight.None;
        private readonly List<RectTransform> highlightedButtons = new List<RectTransform>();
        private readonly List<Canvas> sortingCanvases = new List<Canvas>();
        private RectTransform promptedButton;
        private Canvas rootCanvas;

        // Posiciones originales, para volver a ellas al terminar un resaltado.
        private Vector2 continueHintRestPosition;
        private Vector2 lineTextRestOffsetMin;

        public void OnPointerClick(PointerEventData eventData) => tapped = true;

        private void Awake()
        {
            // Se guardan una sola vez; buscarlos en cada resaltado sería trabajo repetido.
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            continueHintRestPosition = ContinueHintRect.anchoredPosition;
            lineTextRestOffsetMin = lineText.rectTransform.offsetMin;
        }

        // Muestra los mensajes en orden y termina cuando el jugador cierra el último.
        public IEnumerator Play(IReadOnlyList<DialogueLine> lines)
        {
            if (lines == null || lines.Count == 0) yield break;

            // Pausa real: con timeScale = 0 se congelan los gladiadores, sus animaciones y los
            // temporizadores del combate. Al terminar restauramos el valor que había.
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            gameObject.SetActive(true);

            for (int i = 0; i < lines.Count; i++)
            {
                // El primer mensaje (o uno marcado con "Appear Again") hace la entrada del personaje.
                if (i == 0 || lines[i].appearAgain)
                    yield return OpenBox(lines[i], closeFirst: i > 0);
                yield return ShowLine(lines[i]);
            }

            SetHighlight(DialogueHighlight.None);
            gameObject.SetActive(false);
            Time.timeScale = previousTimeScale;
        }

        // Entrada del personaje: (si hace falta) cierra el cuadro un momento, muestra su cara y hace una pausa.
        private IEnumerator OpenBox(DialogueLine line, bool closeFirst)
        {
            if (closeFirst)
            {
                SetBoxVisible(false);
                yield return WaitRealtime(reappearGap);
            }

            SetHighlight(line.highlight);
            lineText.text = string.Empty;
            SetContinueHintVisible(false);
            SetPortrait(line.expression);
            SetBoxVisible(true);
            yield return WaitRealtime(openingPause);
        }

        private IEnumerator ShowLine(DialogueLine line)
        {
            SetHighlight(line.highlight);
            SetPortrait(line.talkWhileWriting ? PortraitExpression.Talk : line.expression);
            SetContinueHintVisible(false);

            tapped = false;
            yield return RevealWords(line.text);
            SetPortrait(line.expression);

            // Tiempo de lectura: los toques acá no cuentan, así nadie se saltea el mensaje sin querer.
            yield return WaitRealtime(readingPause);

            SetContinueHintVisible(true);
            tapped = false;
            while (!tapped)
                yield return null;
        }

        // Texto con fundido palabra por palabra.
        // Se escribe el mensaje una vez y cada frame solo se cambia el alfa de cada letra.

        private IEnumerator RevealWords(string text)
        {
            lineText.text = text;
            lineText.maxVisibleCharacters = int.MaxValue;
            lineText.ForceMeshUpdate(); // genera la malla ya, para poder leer letras y palabras
            MapCharactersToWords();

            // Tiempo total: la última palabra empieza (cantidad - 1) * secondsPerWord y tarda wordFadeSeconds.
            int wordCount = Mathf.Max(1, lineText.textInfo.wordCount);
            float duration = (wordCount - 1) * secondsPerWord + wordFadeSeconds;

            // Un toque mientras aparecen las palabras las muestra todas de una.
            for (float t = 0f; t < duration && !tapped; t += Time.unscaledDeltaTime)
            {
                ApplyWordFade(t);
                yield return null;
            }
            ApplyWordFade(float.MaxValue); // todo visible
        }

        // Calcula a qué palabra pertenece cada letra. TMP nos da dónde empieza cada palabra; los signos
        // sueltos (como "¡") quedan asociados a la palabra que tienen al lado.
        private void MapCharactersToWords()
        {
            var info = lineText.textInfo;
            if (wordOfCharacter.Length < info.characterCount)
                wordOfCharacter = new int[info.characterCount]; // solo crece si el mensaje es más largo

            int word = 0;
            for (int i = 0; i < info.characterCount; i++)
            {
                while (word + 1 < info.wordCount && info.wordInfo[word + 1].firstCharacterIndex <= i)
                    word++;
                wordOfCharacter[i] = word;
            }
        }

        // Pone la transparencia de cada letra según cuánto tiempo pasó desde que empezó su palabra.
        private void ApplyWordFade(float elapsed)
        {
            var info = lineText.textInfo;
            byte baseAlpha = (byte)(lineText.color.a * 255f);

            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue; // los espacios no tienen vértices

                float start = wordOfCharacter[i] * secondsPerWord;
                float progress = Mathf.Clamp01((elapsed - start) / wordFadeSeconds);
                byte alpha = (byte)(baseAlpha * progress);

                var colors = info.meshInfo[character.materialReferenceIndex].colors32;
                for (int v = 0; v < 4; v++)
                    colors[character.vertexIndex + v].a = alpha;
            }

            // Solo subimos los colores a la placa de video, no la malla entera.
            lineText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        //  Resaltado de botones

        // Resalta el botón que el jugador tiene que tocar para seguir (o ninguno con None).
        // Lo usa CombatStarter fuera del diálogo: acá el botón se puede tocar normalmente.
        public void SetPromptHighlight(PlayerIntent intent)
        {
            if (promptedButton != null)
                PulsingHighlight.Remove(promptedButton);

            promptedButton = ButtonFor(intent);
            if (promptedButton != null)
                PulsingHighlight.Add(promptedButton, highlightColor);
        }

        // Resalta durante el diálogo: a cada botón se le agrega un Canvas propio con mayor orden de dibujo.
        // Ese Canvas no tiene GraphicRaycaster, así que el toque lo sigue recibiendo el diálogo.
        private void SetHighlight(DialogueHighlight highlight)
        {
            if (highlight == currentHighlight) return;
            currentHighlight = highlight;
            ClearHighlight();

            CollectButtons(highlight, highlightedButtons);
            foreach (var button in highlightedButtons)
            {
                var canvas = button.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = rootCanvas.sortingOrder + 1;
                sortingCanvases.Add(canvas);
                PulsingHighlight.Add(button, highlightColor);
            }

            // Los botones resaltados se dibujan sobre la parte de abajo del cuadro.
            // El texto se acomoda encima y 'Tocá para seguir' pasa arriba.
            bool on = highlightedButtons.Count > 0;
            lineText.rectTransform.offsetMin = on
                ? new Vector2(lineTextRestOffsetMin.x, Mathf.Max(lineTextRestOffsetMin.y, textBottomWhileHighlighting))
                : lineTextRestOffsetMin;
            ContinueHintRect.anchoredPosition = on
                ? new Vector2(continueHintRestPosition.x, panel.rect.height + 10f)
                : continueHintRestPosition;
        }

        private void ClearHighlight()
        {
            // DestroyImmediate: si en el mismo frame el botón pasa a esperar que lo toquen (fin del diálogo →
            // espera del combate), no puede quedar ni un frame con este Canvas, que lo hace intocable.
            foreach (var canvas in sortingCanvases)
                DestroyImmediate(canvas);
            sortingCanvases.Clear();

            foreach (var button in highlightedButtons)
                PulsingHighlight.Remove(button);
            highlightedButtons.Clear();
        }

        // Llena "result" con los botones que corresponden a ese resaltado (orden: Atacá, Defiéndete, Ríndete).
        private void CollectButtons(DialogueHighlight highlight, List<RectTransform> result)
        {
            switch (highlight)
            {
                case DialogueHighlight.OrderButtons:
                    result.AddRange(orderButtons);
                    break;
                case DialogueHighlight.AttackButton:
                    AddButton(PlayerIntent.Attack, result);
                    break;
                case DialogueHighlight.DefendButton:
                    AddButton(PlayerIntent.Defend, result);
                    break;
                case DialogueHighlight.SurrenderButton:
                    AddButton(PlayerIntent.Surrender, result);
                    break;
            }
        }

        private void AddButton(PlayerIntent intent, List<RectTransform> result)
        {
            var button = ButtonFor(intent);
            if (button != null)
                result.Add(button);
        }

        private RectTransform ButtonFor(PlayerIntent intent)
        {
            int index;
            switch (intent)
            {
                case PlayerIntent.Attack: index = 0; break;
                case PlayerIntent.Defend: index = 1; break;
                case PlayerIntent.Surrender: index = 2; break;
                default: return null;
            }
            return index < orderButtons.Length ? orderButtons[index] : null;
        }

        //  Utilidades

        private RectTransform ContinueHintRect => (RectTransform)continueHint.transform;

        private void SetBoxVisible(bool visible)
        {
            panel.gameObject.SetActive(visible);
            portrait.gameObject.SetActive(visible);
        }

        private void SetPortrait(PortraitExpression expression)
        {
            int index = (int)expression;
            if (index < portraits.Length && portraits[index] != null)
                portrait.sprite = portraits[index];

            // Espejado con escala X negativa. El pivot del retrato está centrado abajo, así que al
            // darlo vuelta no se corre de lugar.
            bool mirrored = System.Array.IndexOf(mirroredExpressions, expression) >= 0;
            portrait.rectTransform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
        }

        // Como WaitForSeconds, pero en tiempo real (WaitForSeconds se congela con el juego en pausa).
        private static IEnumerator WaitRealtime(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                yield return null;
        }

        // Sin "?.": un campo de Unity sin asignar no es null "de verdad" y ese operador no lo detecta.
        private void SetContinueHintVisible(bool visible)
        {
            if (continueHint != null)
                continueHint.SetActive(visible);
        }
    }
}
