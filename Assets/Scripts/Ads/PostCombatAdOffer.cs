using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Despues de ganar un combate ofrece ver un anuncio para ganar denarios de mas.
    // Arma su propia pantalla por codigo y espera a que el jugador elija; el premio lo da AdsService.
    public static class PostCombatAdOffer
    {
        private const string FontPath = "Fonts/Marathon SDF";
        private const float CloseDelaySeconds = 2.4f;

        // Muestra la oferta y espera hasta que se mira el anuncio o se elige continuar.
        public static IEnumerator Offer()
        {
            var ui = new Ui();
            ui.Build();
            EventManager.Subscribe<AdFinishedEvent>(ui.OnAdFinished);

            while (!ui.Finished)
                yield return null;

            EventManager.Unsubscribe<AdFinishedEvent>(ui.OnAdFinished);
            ui.Destroy();
        }

        private sealed class Ui
        {
            private GameObject root;
            private TMP_Text message;
            private GameObject buttons;
            private float closeAt;
            private bool skipped;

            public bool Finished => skipped || (closeAt > 0f && Time.unscaledTime >= closeAt);

            public void Build()
            {
                root = new GameObject("PostCombatAdOffer");
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                root.AddComponent<GraphicRaycaster>();

                var font = Resources.Load<TMP_FontAsset>(FontPath);
                var panel = NewRect("Panel", root.transform);
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
                panel.pivot = new Vector2(0.5f, 0f);
                panel.anchoredPosition = new Vector2(0f, 70f);
                panel.sizeDelta = new Vector2(1100f, 320f);
                var background = panel.gameObject.AddComponent<Image>();
                background.color = new Color(0.07f, 0.04f, 0.04f, 0.93f);
                var border = panel.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.85f, 0.65f, 0.2f, 1f);
                border.effectDistance = new Vector2(4f, -4f);

                message = NewLabel("Mensaje", panel, font, 46f, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.96f));
                message.text = $"¿Querés más? Mirá un anuncio y ganá {DenariiText.Amount(AdsConfig.Instance.coinsIfCompleted, showPlus: true)}";

                var group = NewRect("Botones", panel);
                group.anchorMin = Vector2.zero;
                group.anchorMax = Vector2.one;
                group.offsetMin = group.offsetMax = Vector2.zero;
                buttons = group.gameObject;
                NewButton("VerAnuncio", group, font, "Ver anuncio", new Vector2(0.05f, 0.09f), new Vector2(0.49f, 0.42f),
                    new Color(0.6f, 0.12f, 0.1f), OnWatch);
                NewButton("Continuar", group, font, "Continuar", new Vector2(0.51f, 0.09f), new Vector2(0.95f, 0.42f),
                    new Color(0.28f, 0.28f, 0.3f), () => skipped = true);
            }

            public void Destroy()
            {
                if (root != null) Object.Destroy(root);
            }

            private void OnWatch()
            {
                buttons.SetActive(false);
                message.text = "Cargando anuncio...";
                AdsService.Show(AdSlot.Coins);
            }

            // Cuando termina el anuncio se muestra que premio dio y se cierra solo.
            public void OnAdFinished(AdFinishedEvent e)
            {
                if (e.slot != AdSlot.Coins) return;

                buttons.SetActive(false);
                message.text = e.message;
                closeAt = Time.unscaledTime + CloseDelaySeconds;
            }

            private static RectTransform NewRect(string name, Transform parent)
            {
                var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                return rect;
            }

            private static TMP_Text NewLabel(string name, RectTransform parent, TMP_FontAsset font, float size, Vector2 min, Vector2 max)
            {
                var rect = NewRect(name, parent);
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) label.font = font;
                label.fontSize = size;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
                return label;
            }

            private static void NewButton(string name, RectTransform parent, TMP_FontAsset font, string text, Vector2 min, Vector2 max,
                Color color, UnityEngine.Events.UnityAction onClick)
            {
                var rect = NewRect(name, parent);
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                var image = rect.gameObject.AddComponent<Image>();
                image.color = color;
                var outline = rect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.65f, 0.2f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);

                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(onClick);

                var label = NewLabel("Texto", rect, font, 42f, Vector2.zero, Vector2.one);
                label.text = text;
            }
        }
    }
}
