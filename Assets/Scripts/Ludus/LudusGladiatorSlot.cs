using UnityEngine;
using UnityEngine.EventSystems;

namespace GladiusAI
{
    // Gladiador clickeable/tocable en la Ludus. Avisa a LudusController cual es su indice en el roster.
    // Al pasar el mouse por encima aparece un recuadro redondeado detrás (como "a punto de elegirlo");
    // al elegirlo el recuadro se va y el gladiador queda dorado.
    public class LudusGladiatorSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color LockedTint = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color HighlightTint = new Color(1f, 0.85f, 0.3f);
        private static readonly Color BusyTint = new Color(0.4f, 0.55f, 0.95f);

        [Tooltip("Sprite que se tiñe al estar bloqueado o elegido. Vacío = el del GladiatorNPC.")]
        [SerializeField] private SpriteRenderer characterSprite;

        // Recuadro al pasar el mouse: color, ancho y alto (unidades del mundo) y altura de su centro sobre los pies.
        private static readonly Color hoverColor = new Color(1f, 0.92f, 0.7f, 0.22f);
        private static readonly Vector2 hoverSize = new Vector2(1.7f, 2.45f);
        private const float hoverCenterHeight = 1f;

        private int slotIndex;
        private LudusController controller;
        private bool locked;
        private bool busy;
        private bool highlighted;
        private bool pointerOver;
        private SpriteRenderer hoverFrame;

        private static Sprite roundedSprite;   // uno solo, compartido por todos los gladiadores

        // LudusController lo llama en Start, cuando el GladiatorNPC ya resolvió su sprite en Awake.
        public void Configure(int index, LudusController owner)
        {
            slotIndex = index;
            controller = owner;

            if (characterSprite == null)
            {
                var gladiator = GetComponent<GladiatorNPC>();
                if (gladiator != null)
                    characterSprite = gladiator.CharacterSprite;
            }

            if (hoverFrame == null)
                hoverFrame = CreateHoverFrame();
        }

        // El toque lo detecta el collider del gladiador, que sigue activo aunque su forma no se dibuje.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!locked && !busy) controller.SelectSlot(slotIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOver = true;
            RefreshHoverFrame();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
            RefreshHoverFrame();
        }

        // Oscurece al gladiador y deja de responder al toque mientras no este reclutado.
        public void SetLocked(bool value)
        {
            locked = value;
            SetTint(locked ? LockedTint : Color.white);
            RefreshHoverFrame();
        }

        // Tiñe de azul y deja de responder al toque mientras el gladiador esta entrenando.
        public void SetBusy(bool value)
        {
            busy = value;
            if (!locked)
                SetTint(busy ? BusyTint : Color.white);
            RefreshHoverFrame();
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            if (!locked && !busy)
                SetTint(highlighted ? HighlightTint : Color.white);
            RefreshHoverFrame();
        }

        // El recuadro solo aparece sobre un gladiador que se puede elegir y que todavía no está elegido.
        private void RefreshHoverFrame()
        {
            if (hoverFrame != null)
                hoverFrame.enabled = pointerOver && !locked && !busy && !highlighted;
        }

        // SpriteRenderer.color multiplica los colores del dibujo: blanco lo deja igual, un gris oscuro lo
        // convierte en silueta y un dorado lo "ilumina". No crea materiales nuevos, así que es gratis.
        private void SetTint(Color tint)
        {
            if (characterSprite != null)
                characterSprite.color = tint;
        }

        // ---------- recuadro ----------

        // Se arma por código para no tener que tocar los 8 gladiadores de la escena.
        // Va detrás del dibujo del gladiador, en el mismo plano, y trae su propia zona de detección
        // (un trigger del tamaño del recuadro) porque el collider del cuerpo solo cubre los pies.
        private SpriteRenderer CreateHoverFrame()
        {
            var go = new GameObject("HoverFrame");
            go.layer = gameObject.layer;
            var t = go.transform;
            t.SetParent(transform, false);

            // Mismo plano que el dibujo del gladiador, un poquito más atrás.
            Vector3 basePos = characterSprite != null
                ? transform.InverseTransformPoint(characterSprite.transform.position)
                : Vector3.zero;
            Vector3 scale = transform.lossyScale;
            t.localPosition = new Vector3(0f, basePos.y + hoverCenterHeight / scale.y, basePos.z + 0.05f / scale.z);
            t.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);   // así hoverSize queda en unidades del mundo

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRoundedSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = hoverSize;
            sr.color = hoverColor;
            if (characterSprite != null)
            {
                sr.sharedMaterial = characterSprite.sharedMaterial;
                sr.sortingLayerID = characterSprite.sortingLayerID;
                sr.sortingOrder = characterSprite.sortingOrder - 2;   // detrás del gladiador y de su sombra
            }
            sr.enabled = false;

            var zone = go.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(hoverSize.x, hoverSize.y, 0.2f);

            return sr;
        }

        // Rectángulo blanco con esquinas redondeadas, generado una sola vez (64x64, bordes de 9-slice).
        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;

            const int size = 64;
            const float radius = 20f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distancia al rectángulo interior: dentro vale <= 0, en las esquinas crece hacia afuera.
                float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius), 0f);
                float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius), 0f);
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);   // borde suave
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            roundedSprite.name = "HoverFrame (redondeado)";
            return roundedSprite;
        }
    }
}
