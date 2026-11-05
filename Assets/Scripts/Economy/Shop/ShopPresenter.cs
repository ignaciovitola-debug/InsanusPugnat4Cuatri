using System;

namespace GladiusAI
{
    // Presenter: conecta la pantalla con el Model. Es C# puro, sin nada de Unity.
    public sealed class ShopPresenter : IDisposable
    {
        private readonly IShopView view;
        private readonly ShopModel model;

        public ShopPresenter(IShopView view, ShopModel model)
        {
            this.view = view;
            this.model = model;

            view.BuyRequested += OnBuyRequested;
            view.ShowOffers(model.GetOffers());
        }

        public void Dispose() => view.BuyRequested -= OnBuyRequested;

        private void OnBuyRequested(int index)
        {
            var result = model.TryBuy(index);
            if (result.success)
                view.PlayPurchaseSound();

            view.ShowMessage(result.message);
        }
    }
}
