using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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

            [Header("Tutorial (opcional)")]
            [Tooltip("Si es distinto de None, el combate no arranca hasta que el jugador use ese boton al menos una vez.")]
            public PlayerIntent requiredIntentToStart;
            public bool invulnerable;
            [TextArea] public string tutorialHint;
            [Tooltip("Si se completa este tiempo sin resolver el gate, el cartel cambia a tutorialHintDelayed.")]
            public string tutorialHintDelayed;
            public float tutorialHintDelaySeconds;
        }

        [Header("Factory")]
        [SerializeField] private GladiatorFactory factory; 

        private IGladiatorFactory Factory => factory;

        [Header("Puntos de spawn")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform enemySpawnPoint2; 

        [Header("Oleadas secuenciales (opcional, Nivel 1)")]
        [Tooltip("Si tiene elementos, el nivel spawnea estos enemigos uno tras otro (con estas stats) en vez del enemigo único de arriba.")]
        [SerializeField] private EnemyWave[] waves;

        [Header("Cuenta regresiva")]
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private TMP_Text countdownLabel;
        [Tooltip("Cartel separado del countdownLabel para los mensajes del tutorial (no se pisan entre si).")]
        [SerializeField] private TMP_Text tutorialHintLabel;

        [Header("Consignas del jugador")]
        [SerializeField] private PlayerIntentController intentController;

        [Header("Despues del combate")]
        [Tooltip("Escena a la que se pasa unos segundos despues de terminar el combate (Arena 1 -> Ludus, Arena 2 -> Menu).")]
        [SerializeField] private string nextSceneAfterCombat = "Menu";
        [SerializeField] private float delayBeforeNextScene = 2.5f;
        [Tooltip("Respiro entre vencer a un gladiador y que arranque el siguiente (Nivel 1, oleadas).")]
        [SerializeField] private float delayBetweenWaves = 2f;
        [Tooltip("Activar solo en la Arena 2 -- gana una vez y desbloquea al siguiente gladiador de la Ludus.")]
        [SerializeField] private bool grantsGladiatorUnlock;

        private GladiatorNPC player;
        private GladiatorNPC enemy;
        private GladiatorNPC enemy2;
        private bool retargetedToSecondEnemy;
        private bool singleEncounterEnded;
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
            if (player != null)
            {
                var selected = GladiatorRoster.GetSelected();
                player.ConfigureStats(selected.maxHP, selected.minDamage, selected.maxDamage, selected.attackCooldown);
            }

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

            StartCoroutine(RunCountdown(activateCombatAfter: true));
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

            CheckSingleEncounterOutcome();
        }

        private void CheckSingleEncounterOutcome()
        {
            if (singleEncounterEnded || player == null) return;
            if (waves != null && waves.Length > 0) return; // ese camino lo maneja RunWaveSequence

            if (player.IsDead)
            {
                singleEncounterEnded = true;
                EndCombat(CombatResult.Defeat);
                return;
            }

            if (player.HasSurrendered)
            {
                singleEncounterEnded = true;
                EndCombat(CombatResult.Surrender);
                return;
            }

            bool enemyDefeated = enemy == null || enemy.IsDead;
            bool enemy2Defeated = enemy2 == null || enemy2.IsDead;
            if (enemyDefeated && enemy2Defeated)
            {
                singleEncounterEnded = true;
                EndCombat(CombatResult.Victory);
            }
        }

        /// <summary>Publica el resultado por EventManager y agenda el paso a la siguiente escena.</summary>
        private void EndCombat(CombatResult result)
        {
            if (result == CombatResult.Victory && grantsGladiatorUnlock)
                GladiatorRoster.UnlockNext();

            EventManager.Raise(new CombatEndedEvent(result));
            StartCoroutine(LoadNextSceneAfterDelay());
        }

        private IEnumerator LoadNextSceneAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeNextScene);
            if (string.IsNullOrEmpty(nextSceneAfterCombat)) yield break;

            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(nextSceneAfterCombat);
            }
            else
            {
                Debug.LogWarning("No hay SceneFader activo (¿arrancaste el juego desde Splash?) -- cargando sin fundido.");
                SceneManager.LoadScene(nextSceneAfterCombat);
            }
        }

        private IEnumerator RunCountdown(bool activateCombatAfter)
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

            if (activateCombatAfter)
            {
                player?.SetCombatEnabled(true);
                enemy?.SetCombatEnabled(true);
                enemy2?.SetCombatEnabled(true);
                EventManager.Raise(new CombatStartedEvent());
            }

            yield return new WaitForSeconds(1f);

            if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(false);
        }

        private IEnumerator RunWaveSequence()
        {
            enemyPool = new GladiatorPool(Factory);

            player = Factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            player?.SetIntentController(intentController);
            player?.SetCombatEnabled(false);

            yield return StartCoroutine(RunCountdown(activateCombatAfter: false));

            for (int i = 0; i < waves.Length; i++)
            {
                EnemyWave wave = waves[i];

                if (i > 0 && player != null)
                {
                    player.transform.position = playerSpawnPoint.position;
                    player.FullyHeal();
                }

                enemy = enemyPool.Get(enemySpawnPoint.position, Quaternion.identity);
                enemy?.ConfigureStats(wave.maxHP, wave.minDamage, wave.maxDamage, wave.attackCooldown);
                enemy?.SetInvulnerable(wave.invulnerable);

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

                if (wave.requiredIntentToStart != PlayerIntent.None)
                    yield return StartCoroutine(WaitForRequiredIntent(wave));

                player?.SetCombatEnabled(true);
                enemy?.SetCombatEnabled(true);

                while (enemy != null && !enemy.IsDead && player != null && !player.IsDead && !player.HasSurrendered)
                    yield return null;

                if (player == null || player.IsDead)
                {
                    GladiatorRoster.ResetSlotToBasic(0);
                    EndCombat(CombatResult.Defeat);
                    yield break;
                }

                if (player.HasSurrendered)
                {
                    EndCombat(CombatResult.Surrender);
                    yield break;
                }

                enemyPool.Release(enemy);

                if (i < waves.Length - 1)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }

            EndCombat(CombatResult.Victory);
        }

        /// <summary>Muestra el cartel del tutorial y bloquea el arranque del combate hasta que el jugador use el boton pedido.</summary>
        private IEnumerator WaitForRequiredIntent(EnemyWave wave)
        {
            if (tutorialHintLabel != null)
            {
                tutorialHintLabel.gameObject.SetActive(true);
                tutorialHintLabel.text = wave.tutorialHint;
            }

            float elapsed = 0f;
            bool swapped = false;

            while (intentController == null || intentController.CurrentIntent != wave.requiredIntentToStart)
            {
                elapsed += Time.deltaTime;
                if (!swapped && !string.IsNullOrEmpty(wave.tutorialHintDelayed) && elapsed >= wave.tutorialHintDelaySeconds)
                {
                    swapped = true;
                    if (tutorialHintLabel != null)
                        tutorialHintLabel.text = wave.tutorialHintDelayed;
                }
                yield return null;
            }

            intentController?.ClearIntent();

            if (tutorialHintLabel != null)
                tutorialHintLabel.gameObject.SetActive(false);
        }
    }
}
