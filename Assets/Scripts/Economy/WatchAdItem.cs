using UnityEngine;

namespace GladiusAI
{
    // Item gratis de la tienda: se mira un anuncio y se recupera energia. Si se ve entero se llena;
    // si se cierra antes, da solo un poco (lo decide AdRewards).
    [CreateAssetMenu(fileName = "WatchEnergyAd", menuName = "Insanus Pugnat/Shop/Watch Ad (Energy)")]
    public class WatchAdItem : ShopItem
    {
        public override string PriceText => "Gratis";

        public override string PurchaseMessage => "Mirá el anuncio hasta el final para llenar la energía.";

        public override bool CanBuy(out string reason)
        {
            if (Energy.Current >= Energy.Max)
                reason = "La energía ya está llena";
            else if (!AdsService.IsReady(AdSlot.Energy))
                reason = "El anuncio todavía no está listo, probá en unos segundos";
            else
                reason = null;

            return reason == null;
        }

        public override void Apply() => AdsService.Show(AdSlot.Energy);
    }
}
