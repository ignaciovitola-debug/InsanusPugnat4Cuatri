using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GladiusAI
{
    // View de la tienda: solo muestra las filas y avisa cuando tocan comprar.
    public class ShopView : MonoBehaviour, IShopView
    {
        [SerializeField] private RectTransform content;
        [Tooltip("Fila de molde: queda apagada y se copia una vez por item.")]
        [SerializeField] private ShopRow rowTemplate;
        [SerializeField] private TMP_Text messageLabel;

        public event Action<int> BuyRequested;

        private void OnEnable() => ShowMessage(string.Empty);

        public void ShowOffers(IReadOnlyList<ShopOffer> offers)
        {
            rowTemplate.gameObject.SetActive(false);

            for (int i = 0; i < offers.Count; i++)
            {
                int index = i;
                var row = Instantiate(rowTemplate, content);
                row.gameObject.SetActive(true);
                row.Setup(offers[i], () => BuyRequested?.Invoke(index));
            }
        }

        public void ShowMessage(string text)
        {
            if (messageLabel != null)
                messageLabel.text = text;
        }

        public void PlayPurchaseSound() => SoundManager.Play(SoundId.CrowdCheer);
    }
}
