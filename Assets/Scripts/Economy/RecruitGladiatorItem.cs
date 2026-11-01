using UnityEngine;

namespace GladiusAI
{
    [CreateAssetMenu(fileName = "RecruitGladiator", menuName = "Insanus Pugnat/Shop/Recruit Gladiator")]
    public class RecruitGladiatorItem : ShopItem
    {
        public override bool CanBuy(out string reason)
        {
            reason = GladiatorRoster.HasLockedSlot ? null : "Ya tenés a todos los gladiadores";
            return reason == null;
        }

        public override void Apply() => GladiatorRoster.UnlockNext();
    }
}
