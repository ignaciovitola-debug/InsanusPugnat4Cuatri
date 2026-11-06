using System.Collections.Generic;

namespace GladiusAI
{
    public readonly struct PurchaseResult
    {
        public readonly bool success;
        public readonly string message;

        public PurchaseResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }

    // Model de la tienda: guarda los items y decide si una compra se puede hacer. No conoce la pantalla.
    public class ShopModel
    {
        private readonly List<ShopItem> items = new List<ShopItem>();

        public ShopModel(IEnumerable<ShopItem> source)
        {
            foreach (var item in source)
                if (item != null) items.Add(item);
        }

        public List<ShopOffer> GetOffers()
        {
            var offers = new List<ShopOffer>(items.Count);
            foreach (var item in items)
                offers.Add(new ShopOffer(item.DisplayName, item.Description, item.Price));
            return offers;
        }

        // Revisa que el item este disponible, cobra los denarios y lo aplica.
        public PurchaseResult TryBuy(int index)
        {
            if (index < 0 || index >= items.Count)
                return new PurchaseResult(false, "Ese item no existe");

            var item = items[index];
            if (!item.CanBuy(out string reason))
                return new PurchaseResult(false, reason);

            if (!Denarii.TrySpend(item.Price))
                return new PurchaseResult(false, $"Te faltan {DenariiText.Amount(item.Price - Denarii.Balance)}");

            item.Apply();
            return new PurchaseResult(true, $"¡Compraste {item.DisplayName}!");
        }
    }
}
