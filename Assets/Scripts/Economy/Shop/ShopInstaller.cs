using UnityEngine;

namespace GladiusAI
{
    // Arma la tienda: crea el Model y el Presenter y los conecta con la pantalla.
    public class ShopInstaller : MonoBehaviour
    {
        [SerializeField] private ShopView view;
        [SerializeField] private ShopItem[] items = new ShopItem[0];

        private ShopPresenter presenter;

        private void Awake() => presenter = new ShopPresenter(view, new ShopModel(items));

        private void OnDestroy() => presenter?.Dispose();
    }
}
