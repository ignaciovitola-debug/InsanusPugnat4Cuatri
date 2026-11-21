using UnityEngine;

namespace GladiusAI
{
    // Resultado de un entrenamiento: cuánto suben vida y ataque, y si hubo premios extra.
    public readonly struct TrainingOutcome
    {
        public readonly float bonusHP;
        public readonly float bonusDamage;
        public readonly bool doubled;
        public readonly bool guardUp;

        public TrainingOutcome(float bonusHP, float bonusDamage, bool doubled, bool guardUp)
        {
            this.bonusHP = bonusHP;
            this.bonusDamage = bonusDamage;
            this.doubled = doubled;
            this.guardUp = guardUp;
        }

        // Siempre sube al menos 1 punto de vida y de ataque. Los dos números al azar (0 a 1) se pasan de afuera.
        public static TrainingOutcome Roll(EconomyConfig.TrainingTier tier, float levelScale, float doubleRoll, float guardRoll)
        {
            float hp = Mathf.Max(1f, tier.bonusHP * levelScale);
            float damage = Mathf.Max(1f, tier.bonusDamage * levelScale);

            bool doubled = doubleRoll < tier.doubleChance;
            if (doubled)
            {
                hp *= 2f;
                damage *= 2f;
            }

            return new TrainingOutcome(hp, damage, doubled, guardRoll < tier.guardChance);
        }

        public string Describe()
        {
            string text = $"+{bonusHP:0} vida, +{bonusDamage:0.#} ataque";
            if (doubled) text += " ¡DOBLE!";
            if (guardUp) text += " ¡Guardia mejorada!";
            return text;
        }
    }
}
