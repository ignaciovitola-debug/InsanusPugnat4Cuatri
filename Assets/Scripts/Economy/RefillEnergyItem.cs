using UnityEngine;

namespace GladiusAI
{
    [CreateAssetMenu(fileName = "RefillEnergy", menuName = "Insanus Pugnat/Shop/Refill Energy")]
    public class RefillEnergyItem : ShopItem
    {
        public override bool CanBuy(out string reason)
        {
            reason = Energy.Current < Energy.Max ? null : "La energía ya está llena";
            return reason == null;
        }

        public override void Apply() => Energy.Refill();
    }
}
