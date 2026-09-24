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

        public void OnPointerClick(PointerEventData eventData) => controller.SelectSlot(slotIndex);

        public void SetHighlighted(bool highlighted)
        {
            if (bodyRenderer != null)
                bodyRenderer.material.color = highlighted ? Color.yellow : Color.white;
        }
    }
}
