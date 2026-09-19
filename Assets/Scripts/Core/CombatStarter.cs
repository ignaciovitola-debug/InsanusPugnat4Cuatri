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
        [SerializeField] private GladiatorFactory factory; // Unity no serializa interfaces en el Inspector; el resto del código usa IGladiatorFactory.

        private IGladiatorFactory Factory => factory;

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
        private GladiatorPool enemyPool;

        private void Start()
        {
            if (waves != null && waves.Length > 0)
                StartCoroutine(RunWaveSequence());
            else
                StartSingleEncounter();
        }

        private void StartSingleEncounter()
        {
            player = Factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            enemy = Factory.CreateEnemy(enemySpawnPoint.position, Quaternion.identity);

            if (player != null && enemy != null)
            {
                player.SetTarget(enemy.transform);
                enemy.SetTarget(player.transform);
            }

            if (enemySpawnPoint2 != null)
            {
                enemy2 = Factory.CreateEnemy(enemySpawnPoint2.position, Quaternion.identity);
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
            EventManager.Raise(new CombatStartedEvent());

            yield return new WaitForSeconds(1f);

            if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(false);
        }

        // ==================== Oleadas secuenciales (Nivel 1) ====================
        private IEnumerator RunWaveSequence()
        {
            enemyPool = new GladiatorPool(Factory);

            player = Factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            player?.SetIntentController(intentController);
            player?.SetCombatEnabled(false);

            yield return StartCoroutine(InitialCountdown());

            for (int i = 0; i < waves.Length; i++)
            {
                EnemyWave wave = waves[i];

                enemy = enemyPool.Get(enemySpawnPoint.position, Quaternion.identity);
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
                    EventManager.Raise(new CombatEndedEvent(CombatResult.Defeat));
                    yield break;
                }

                if (player.HasSurrendered)
                {
                    EventManager.Raise(new CombatEndedEvent(CombatResult.Surrender));
                    yield break;
                }

                enemyPool.Release(enemy);
            }

            EventManager.Raise(new CombatEndedEvent(CombatResult.Victory));
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
