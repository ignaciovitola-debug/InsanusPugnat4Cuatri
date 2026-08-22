using UnityEngine;

namespace GladiusAI
{
    public class CombatSimulator : MonoBehaviour
    {
        [Header("Cantidad de peleas")]
        [SerializeField] private int numFights = 1000;

        [Header("Gladiator A")]
        [SerializeField] private float hpA = 100f;
        [SerializeField] private float minDmgA = 10f;
        [SerializeField] private float maxDmgA = 20f;
        [SerializeField] private float cooldownA = 1.2f;
        [SerializeField] private float staggerA = 0.4f;

        [Header("Gladiator B")]
        [SerializeField] private float hpB = 100f;
        [SerializeField] private float minDmgB = 10f;
        [SerializeField] private float maxDmgB = 20f;
        [SerializeField] private float cooldownB = 1.2f;
        [SerializeField] private float staggerB = 0.4f;

        [Header("Simulación")]
        [SerializeField] private float simulatedDeltaTime = 0.02f;
        [SerializeField] private int maxTicksPerFight = 50000;

        [ContextMenu("Correr Simulación")]
        public void RunSimulation()
        {
            int winsA = 0;
            int winsB = 0;
            float totalHitsA = 0;
            float totalHitsB = 0;
            float totalDmgDealtA = 0;
            float totalDmgDealtB = 0;
            float totalDuration = 0;
            float totalWinnerHP = 0;
            float minWinnerHP = float.MaxValue;
            float maxWinnerHP = 0;

            for (int fight = 0; fight < numFights; fight++)
            {
                float curHpA = hpA;
                float curHpB = hpB;
                float cdA = 0f;
                float cdB = 0f;
                float stunA = 0f;
                float stunB = 0f;
                int hitsA = 0;
                int hitsB = 0;
                float dmgDealtA = 0;
                float dmgDealtB = 0;
                int ticks = 0;

                while (curHpA > 0 && curHpB > 0 && ticks < maxTicksPerFight)
                {
                    ticks++;
                    float dt = simulatedDeltaTime;

                    if (cdA > 0) cdA -= dt;
                    if (cdB > 0) cdB -= dt;
                    if (stunA > 0) stunA -= dt;
                    if (stunB > 0) stunB -= dt;

                    if (stunA <= 0 && cdA <= 0 && curHpA > 0 && curHpB > 0)
                    {
                        float dmg = Mathf.Round(Random.Range(minDmgA, maxDmgA));
                        curHpB = Mathf.Max(0, curHpB - dmg);
                        cdA = cooldownA;
                        stunB = staggerB;
                        hitsA++;
                        dmgDealtA += dmg;
                    }

                    if (stunB <= 0 && cdB <= 0 && curHpB > 0 && curHpA > 0)
                    {
                        float dmg = Mathf.Round(Random.Range(minDmgB, maxDmgB));
                        curHpA = Mathf.Max(0, curHpA - dmg);
                        cdB = cooldownB;
                        stunA = staggerA;
                        hitsB++;
                        dmgDealtB += dmg;
                    }
                }

                if (curHpA > 0) winsA++;
                else winsB++;

                float winnerHP = curHpA > 0 ? curHpA : curHpB;
                totalWinnerHP += winnerHP;
                if (winnerHP < minWinnerHP) minWinnerHP = winnerHP;
                if (winnerHP > maxWinnerHP) maxWinnerHP = winnerHP;

                totalHitsA += hitsA;
                totalHitsB += hitsB;
                totalDmgDealtA += dmgDealtA;
                totalDmgDealtB += dmgDealtB;
                totalDuration += ticks * simulatedDeltaTime;
            }

            float avgHitsA = totalHitsA / numFights;
            float avgHitsB = totalHitsB / numFights;
            float avgDmgA = totalDmgDealtA / numFights;
            float avgDmgB = totalDmgDealtB / numFights;
            float avgDuration = totalDuration / numFights;
            float avgWinnerHP = totalWinnerHP / numFights;

            Debug.Log("╔══════════════════════════════════════════════════════╗");
            Debug.Log($"║  SIMULACIÓN DE COMBATE: {numFights} peleas");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  A: HP={hpA} | Dmg={minDmgA}-{maxDmgA} | CD={cooldownA}s | Stagger={staggerA}s");
            Debug.Log($"║  B: HP={hpB} | Dmg={minDmgB}-{maxDmgB} | CD={cooldownB}s | Stagger={staggerB}s");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  A gana: {winsA} ({winsA * 100f / numFights:F1}%)");
            Debug.Log($"║  B gana: {winsB} ({winsB * 100f / numFights:F1}%)");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  Golpes promedio  A: {avgHitsA:F1}  |  B: {avgHitsB:F1}");
            Debug.Log($"║  Daño promedio    A: {avgDmgA:F1}  |  B: {avgDmgB:F1}");
            Debug.Log($"║  Duración promedio: {avgDuration:F1}s");
            Debug.Log("╠══════════════════════════════════════════════════════╣");
            Debug.Log($"║  HP ganador — Prom: {avgWinnerHP:F1} | Min: {minWinnerHP} | Max: {maxWinnerHP}");
            Debug.Log("╚══════════════════════════════════════════════════════╝");

            
        }
    }
}
