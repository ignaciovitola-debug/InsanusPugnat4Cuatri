using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Una fila de la tienda: nombre, descripcion, precio y boton de compra.
    public class ShopRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button buyButton;

        public void Setup(ShopItem item, Action<ShopItem> onBuy)
        {
            nameLabel.text = item.DisplayName;
            descriptionLabel.text = item.Description;
            priceLabel.text = $"{item.Price} denarios";

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy(item));
        }
    }
}
