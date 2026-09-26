using UnityEngine;
using UnityEngine.EventSystems;

namespace GladiusAI
{
    /// <summary>Gladiador clickeable/tocable en la Ludus. Avisa a LudusController cual es su indice en el roster.</summary>
    public class LudusGladiatorSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Renderer bodyRenderer;

        private int slotIndex;
        private LudusController controller;
        private bool locked;

        public void Configure(int index, LudusController owner)
        {
            slotIndex = index;
            controller = owner;
        }

        private void Awake()
        {
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!locked) controller.SelectSlot(slotIndex);
        }

        /// <summary>Tine de gris y deja de responder al toque mientras el gladiador no este reclutado.</summary>
        public void SetLocked(bool value)
        {
            locked = value;
            if (bodyRenderer != null)
                bodyRenderer.material.color = locked ? new Color(0.15f, 0.15f, 0.15f) : Color.white;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (locked || bodyRenderer == null) return;
            bodyRenderer.material.color = highlighted ? Color.yellow : Color.white;
        }
    }
}
