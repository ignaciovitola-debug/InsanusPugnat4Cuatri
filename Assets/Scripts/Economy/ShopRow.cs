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

        public void Setup(ShopOffer offer, Action onBuy)
        {
            nameLabel.text = offer.name;
            descriptionLabel.text = offer.description;
            priceLabel.text = DenariiText.Amount(offer.price);

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy());
        }
    }
}
