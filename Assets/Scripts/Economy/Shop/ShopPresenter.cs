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
            EventManager.Subscribe<AdFinishedEvent>(OnAdFinished);
            view.ShowOffers(model.GetOffers());
        }

        public void Dispose()
        {
            view.BuyRequested -= OnBuyRequested;
            EventManager.Unsubscribe<AdFinishedEvent>(OnAdFinished);
        }

        // El anuncio es asincrono: cuando termina, la tienda muestra que premio dio.
        private void OnAdFinished(AdFinishedEvent e) => view.ShowMessage(e.message);

        private void OnBuyRequested(int index)
        {
            var result = model.TryBuy(index);
            if (result.success)
                view.PlayPurchaseSound();

            view.ShowMessage(result.message);
        }
    }
}
