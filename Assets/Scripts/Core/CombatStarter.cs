using UnityEngine;
using System.Collections;
using TMPro;

namespace GladiusAI
{
    public class CombatStarter : MonoBehaviour
    {
        [System.Serializable]
        public struct EnemyWave
        {
            public string label;
            public float maxHP;
            public float minDamage;
            public float maxDamage;
            public float attackCooldown;
        }

        [Header("Factory")]
        [SerializeField] private GladiatorFactory factory;

        [Header("Puntos de spawn")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform enemySpawnPoint2; // opcional: si se asigna, spawnea un segundo enemigo simultáneo (Nivel 2)

        [Header("Oleadas secuenciales (opcional, Nivel 1)")]
        [Tooltip("Si tiene elementos, el nivel spawnea estos enemigos uno tras otro (con estas stats) en vez del enemigo único de arriba.")]
        [SerializeField] private EnemyWave[] waves;

        [Header("Cuenta regresiva")]
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private TMP_Text countdownLabel;

        [Header("Consignas del jugador")]
        [SerializeField] private PlayerIntentController intentController;

        private GladiatorNPC player;
        private GladiatorNPC enemy;
        private GladiatorNPC enemy2;
        private bool retargetedToSecondEnemy;

        private void Start()
        {
            if (waves != null && waves.Length > 0)
                StartCoroutine(RunWaveSequence());
            else
                StartSingleEncounter();
        }

        private void StartSingleEncounter()
        {
            player = factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            enemy = factory.CreateEnemy(enemySpawnPoint.position, Quaternion.identity);

            if (player != null && enemy != null)
            {
                player.SetTarget(enemy.transform);
                enemy.SetTarget(player.transform);
            }

            if (enemySpawnPoint2 != null)
            {
                enemy2 = factory.CreateEnemy(enemySpawnPoint2.position, Quaternion.identity);
                if (player != null && enemy2 != null)
                    enemy2.SetTarget(player.transform);
                enemy2?.SetCombatEnabled(false);
            }

            player?.SetIntentController(intentController);

            player?.SetCombatEnabled(false);
            enemy?.SetCombatEnabled(false);

            StartCoroutine(CountdownAndBegin());
        }

        private void Update()
        {
            // Nivel 2: cuando el primer enemigo cae, el jugador pasa a enfrentar al segundo.
            if (!retargetedToSecondEnemy && enemy2 != null && player != null &&
                enemy != null && enemy.IsDead && !enemy2.IsDead)
            {
                player.SetTarget(enemy2.transform);
                retargetedToSecondEnemy = true;
            }
        }

        private IEnumerator CountdownAndBegin()
        {
            float remaining = countdownSeconds;

            while (remaining > 0f)
            {
                if (countdownLabel != null)
                    countdownLabel.text = Mathf.CeilToInt(remaining).ToString();

                yield return null;
                remaining -= Time.deltaTime;
            }

            if (countdownLabel != null)
                countdownLabel.text = "¡FIGHT!";

            player?.SetCombatEnabled(true);
            enemy?.SetCombatEnabled(true);
            enemy2?.SetCombatEnabled(true);

            yield return new WaitForSeconds(1f);

            if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(false);
        }

        // ==================== Oleadas secuenciales (Nivel 1) ====================
        private IEnumerator RunWaveSequence()
        {
            player = factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            player?.SetIntentController(intentController);
            player?.SetCombatEnabled(false);

            yield return StartCoroutine(InitialCountdown());

            for (int i = 0; i < waves.Length; i++)
            {
                EnemyWave wave = waves[i];

                enemy = factory.CreateEnemy(enemySpawnPoint.position, Quaternion.identity);
                enemy?.ConfigureStats(wave.maxHP, wave.minDamage, wave.maxDamage, wave.attackCooldown);

                if (player != null && enemy != null)
                {
                    player.SetTarget(enemy.transform);
                    enemy.SetTarget(player.transform);
                }

                if (countdownLabel != null && !string.IsNullOrEmpty(wave.label))
                {
                    countdownLabel.gameObject.SetActive(true);
                    countdownLabel.text = wave.label;
                    yield return new WaitForSeconds(1.5f);
                    countdownLabel.gameObject.SetActive(false);
                }

                player?.SetCombatEnabled(true);
                enemy?.SetCombatEnabled(true);

                while (enemy != null && !enemy.IsDead && player != null && !player.IsDead && !player.HasSurrendered)
                    yield return null;

                if (player == null || player.IsDead)
                {
                    ShowResultBanner("DERROTA — Tu gladiador cayó en combate");
                    yield break;
                }

                if (player.HasSurrendered)
                {
                    ShowResultBanner("Te rendiste");
                    yield break;
                }
            }

            ShowResultBanner("¡VICTORIA!");
        }

        private void ShowResultBanner(string message)
        {
            if (countdownLabel == null) return;
            countdownLabel.gameObject.SetActive(true);
            countdownLabel.text = message;
        }

        private IEnumerator InitialCountdown()
        {
            float remaining = countdownSeconds;

            while (remaining > 0f)
            {
                if (countdownLabel != null)
                    countdownLabel.text = Mathf.CeilToInt(remaining).ToString();

                yield return null;
                remaining -= Time.deltaTime;
            }

            if (countdownLabel != null)
                countdownLabel.text = "¡FIGHT!";

            yield return new WaitForSeconds(1f);

            if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(false);
        }
    }
}
