using UnityEngine;
using UnityEngine.EventSystems;

namespace GladiusAI
{
    /// Gladiador clickeable/tocable en la Ludus. Avisa a LudusController cual es su indice en el roster.
    public class LudusGladiatorSlot : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color LockedTint = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color HighlightTint = new Color(1f, 0.85f, 0.3f);

        [Tooltip("Sprite del personaje que se tiñe al estar bloqueado o elegido. Si queda vacío se usa el del GladiatorNPC.")]
        [SerializeField] private SpriteRenderer characterSprite;

        private int slotIndex;
        private LudusController controller;
        private bool locked;

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
        }

        // El toque lo detecta el collider del gladiador, que sigue activo aunque su forma no se dibuje.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!locked) controller.SelectSlot(slotIndex);
        }

        /// Oscurece al gladiador y deja de responder al toque mientras no este reclutado.
        public void SetLocked(bool value)
        {
            locked = value;
            SetTint(locked ? LockedTint : Color.white);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (!locked)
                SetTint(highlighted ? HighlightTint : Color.white);
        }

        // SpriteRenderer.color multiplica los colores del dibujo: blanco lo deja igual, un gris oscuro lo
        // convierte en silueta y un dorado lo "ilumina". No crea materiales nuevos, así que es gratis.
        private void SetTint(Color tint)
        {
            if (characterSprite != null)
                characterSprite.color = tint;
        }
    }
}
