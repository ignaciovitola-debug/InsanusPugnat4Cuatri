using UnityEngine;

namespace GladiusAI
{
    // Flyweight: los datos que comparten todos los rivales del mismo tipo (un solo asset en memoria).
    // Lo propio de cada rival (vida actual, posicion) queda en su GladiatorNPC.
    [CreateAssetMenu(fileName = "NewArchetype", menuName = "Insanus Pugnat/Enemy Archetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [SerializeField] private string displayName = "Rival";
        [SerializeField] private float maxHP = 90f;
        [SerializeField] private float minDamage = 8f;
        [SerializeField] private float maxDamage = 16f;
        [SerializeField] private float attackCooldown = 1.3f;
        [Tooltip("Aspecto en GladiatorSkins. -1 = al azar.")]
        [SerializeField] private int skinIndex = -1;

        public string DisplayName => displayName;
        public int SkinIndex => skinIndex;

        public CombatStats BaseStats => new CombatStats(maxHP, minDamage, maxDamage, attackCooldown);
    }
}
