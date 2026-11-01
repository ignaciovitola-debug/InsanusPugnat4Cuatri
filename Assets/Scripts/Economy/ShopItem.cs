using UnityEngine;

namespace GladiusAI
{
    // Algo que se puede comprar. Cada tipo de item es una clase nueva que hereda de esta:
    // la tienda solo conoce ShopItem, asi que agregar un item no obliga a tocar su codigo.
    public abstract class ShopItem : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private int price = 100;

        public string DisplayName => displayName;
        public string Description => description;
        public int Price => price;

        // Si no se puede comprar ahora (aunque alcance la plata), explica por que.
        public abstract bool CanBuy(out string reason);

        public abstract void Apply();
    }
}
