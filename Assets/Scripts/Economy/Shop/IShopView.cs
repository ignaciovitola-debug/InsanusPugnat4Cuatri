using System;
using System.Collections.Generic;

namespace GladiusAI
{
    // Lo que el Presenter le puede pedir a la pantalla de la tienda. La pantalla no decide nada.
    public interface IShopView
    {
        // Avisa que se toco el boton de comprar de la fila con ese numero.
        event Action<int> BuyRequested;

        void ShowOffers(IReadOnlyList<ShopOffer> offers);
        void ShowMessage(string text);
        void PlayPurchaseSound();
    }
}
