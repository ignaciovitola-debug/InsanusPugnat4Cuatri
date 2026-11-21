using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Barras de vida a los costados de la pantalla: tu gladiador a la izquierda y el rival a la derecha.
    // Se crea sola cuando un combate avisa quienes pelean, asi que no hay que ponerla en la escena.
    public class HealthBarsHud : MonoBehaviour
    {
        private const string FontPath = "Fonts/Marathon SDF";
        private const float BarWidth = 36f;
        private const float BarHeight = 300f;
        private const float EdgeMargin = 24f;
        private const float Padding = 4f;
        private const float FollowSpeed = 9f;
        private const float TrailSpeed = 2.5f;

        private static HealthBarsHud instance;

        private RectTransform safeRoot;
        private Rect lastSafeArea;
        private Bar playerBar;
        private Bar enemyBar;
        private GladiatorNPC player;
        private GladiatorNPC enemy;
        private GladiatorNPC enemy2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            instance = null;
            EventManager.Unsubscribe<CombatantsChangedEvent>(OnCombatantsChanged);
            EventManager.Subscribe<CombatantsChangedEvent>(OnCombatantsChanged);
        }

        private static void OnCombatantsChanged(CombatantsChangedEvent e)
        {
            if (instance == null)
                instance = Create();
            instance.Bind(e);
        }

        private static HealthBarsHud Create()
        {
            var go = new GameObject("HealthBarsHud");

            // Va por debajo de los botones y de los dialogos del resto de la interfaz.
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -20;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = go.AddComponent<HealthBarsHud>();
            hud.Build();
            return hud;
        }

        private void Build()
        {
            var rootGo = new GameObject("SafeArea", typeof(RectTransform));
            safeRoot = rootGo.GetComponent<RectTransform>();
            safeRoot.SetParent(transform, false);
            ApplySafeArea();

            var font = Resources.Load<TMP_FontAsset>(FontPath);
            playerBar = new Bar(safeRoot, font, leftSide: true,
                new Color(0.88f, 0.25f, 0.2f), new Color(0.3f, 0.85f, 0.35f));
            enemyBar = new Bar(safeRoot, font, leftSide: false,
                new Color(0.55f, 0.1f, 0.1f), new Color(0.9f, 0.3f, 0.2f));
        }

        private void Bind(CombatantsChangedEvent e)
        {
            player = e.player;
            enemy = e.enemy;
            enemy2 = e.enemy2;
            playerBar.SetName(e.playerName);
            enemyBar.SetName(e.enemyName);

            // Un rival nuevo arranca con la barra llena, sin animar la subida.
            playerBar.Snap(Ratio(player, null, out _, out _));
            enemyBar.Snap(Ratio(enemy, enemy2, out _, out _));
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea)
                ApplySafeArea();

            float dt = Time.unscaledDeltaTime;
            playerBar.Tick(Ratio(player, null, out float pc, out float pm), pc, pm, dt);
            enemyBar.Tick(Ratio(enemy, enemy2, out float ec, out float em), ec, em, dt);
        }

        // Vida actual sobre vida maxima de uno o dos gladiadores juntos.
        private static float Ratio(GladiatorNPC a, GladiatorNPC b, out float current, out float max)
        {
            current = 0f;
            max = 0f;
            if (a != null) { current += a.CurrentHP; max += a.MaxHP; }
            if (b != null) { current += b.CurrentHP; max += b.MaxHP; }
            return max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        // Respeta las muescas y bordes redondeados del celular.
        private void ApplySafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        // Una barra vertical con su nombre arriba y el numero de vida abajo.
        private class Bar
        {
            private readonly RectTransform fill;
            private readonly RectTransform trail;
            private readonly Image fillImage;
            private readonly TMP_Text nameLabel;
            private readonly TMP_Text valueLabel;
            private readonly Color lowColor;
            private readonly Color highColor;
            private float shown = 1f;
            private float trailShown = 1f;

            public Bar(RectTransform parent, TMP_FontAsset font, bool leftSide, Color lowColor, Color highColor)
            {
                this.lowColor = lowColor;
                this.highColor = highColor;

                var frame = NewRect("Barra", parent);
                float side = leftSide ? 0f : 1f;
                frame.anchorMin = frame.anchorMax = new Vector2(side, 0.5f);
                frame.pivot = new Vector2(side, 0.5f);
                frame.sizeDelta = new Vector2(BarWidth, BarHeight);
                frame.anchoredPosition = new Vector2(leftSide ? EdgeMargin : -EdgeMargin, 0f);
                AddImage(frame, new Color(0f, 0f, 0f, 0.7f));

                var slot = NewRect("Hueco", frame);
                Stretch(slot, Padding);

                trail = NewRect("Estela", slot);
                AddImage(trail, new Color(1f, 1f, 1f, 0.55f));
                fill = NewRect("Vida", slot);
                fillImage = AddImage(fill, highColor);

                // El nombre usa la fuente romana; los numeros la fuente normal (la romana dibuja el cero con una barra).
                nameLabel = NewLabel("Nombre", frame, font, leftSide, above: true, 32f);
                valueLabel = NewLabel("Valor", frame, null, leftSide, above: false, 28f);
                Apply(1f, 1f, 0f, 0f);
            }

            public void SetName(string text) => nameLabel.text = text;

            public void Snap(float ratio)
            {
                shown = trailShown = ratio;
                Apply(ratio, ratio, 0f, 0f);
            }

            public void Tick(float ratio, float current, float max, float dt)
            {
                shown = Mathf.Lerp(shown, ratio, 1f - Mathf.Exp(-FollowSpeed * dt));
                // La estela blanca se queda un poco atras para mostrar cuanto se perdio.
                trailShown = Mathf.Max(shown, Mathf.Lerp(trailShown, ratio, 1f - Mathf.Exp(-TrailSpeed * dt)));
                Apply(shown, trailShown, current, max);
            }

            private void Apply(float ratio, float trailRatio, float current, float max)
            {
                fill.anchorMax = new Vector2(1f, ratio);
                trail.anchorMax = new Vector2(1f, trailRatio);
                fillImage.color = Color.Lerp(lowColor, highColor, ratio);
                if (max > 0f)
                    valueLabel.text = $"{current:0}/{max:0}";
            }

            private static RectTransform NewRect(string name, RectTransform parent)
            {
                var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                return rect;
            }

            private static void Stretch(RectTransform rect, float padding)
            {
                rect.offsetMin = new Vector2(padding, padding);
                rect.offsetMax = new Vector2(-padding, -padding);
            }

            private static Image AddImage(RectTransform rect, Color color)
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            // El nombre va arriba y el numero abajo, pegados al borde de la pantalla de su lado.
            private static TMP_Text NewLabel(string name, RectTransform bar, TMP_FontAsset font, bool leftSide, bool above, float size)
            {
                var rect = NewRect(name, bar);
                float x = leftSide ? 0f : 1f;
                float y = above ? 1f : 0f;
                rect.anchorMin = rect.anchorMax = new Vector2(x, y);
                rect.pivot = new Vector2(x, above ? 0f : 1f);
                rect.sizeDelta = new Vector2(420f, size + 14f);
                rect.anchoredPosition = new Vector2(0f, above ? 8f : -8f);

                var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) label.font = font;
                label.fontSize = size;
                label.color = Color.white;
                label.alignment = leftSide ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.outlineWidth = 0.25f;
                label.outlineColor = Color.black;
                label.raycastTarget = false;
                return label;
            }
        }
    }
}
