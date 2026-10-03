using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    /// Botones de orden habilitados en un combate. Nada marcado = todos.
    [System.Flags]
    public enum OrderSet
    {
        Attack = 1 << 0,
        Defend = 1 << 1,
        Surrender = 1 << 2,
    }

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
            [Tooltip("Botones de orden que se ven en este combate (el tutorial los va habilitando de a uno). Nothing = todos.")]
            public OrderSet availableOrders;
            [Tooltip("Lo que dice el instructor después del título, antes de que arranque el combate.")]
            public DialogueLine[] introDialogue;
            [Tooltip("Si es distinto de None, el combate no arranca hasta que el jugador toque ese botón.")]
            public PlayerIntent requiredIntentToStart;
            [Tooltip("Cartel fijo mientras se espera ese botón (ej: \"¡Tocá Atacá!\").")]
            [TextArea] public string tutorialHint;
            public bool invulnerable;
            [Tooltip("Botón que queda resaltado (y se puede tocar) durante toda la pelea, hasta que termina.")]
            public PlayerIntent highlightDuringFight;
            [Tooltip("Lo que dice el instructor en medio del combate, a los midFightDelaySeconds de pelea (el combate se pausa).")]
            public DialogueLine[] midFightDialogue;
            public float midFightDelaySeconds;
            [Tooltip("Cartel fijo que queda después de ese diálogo, hasta que termina el combate (ej: \"¡Tocá Ríndete!\").")]
            [TextArea] public string midFightHint;
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

        [Header("Tutorial (opcional, Nivel 1)")]
        [SerializeField] private TutorialDialogue dialogue;
        [Tooltip("En este orden: Atacá, Defiéndete, Ríndete. Cada oleada muestra solo los de su availableOrders.")]
        [SerializeField] private GameObject[] orderButtons = new GameObject[0];
        [Tooltip("Lo que dice el instructor al entrar a la arena, antes de la cuenta regresiva.")]
        [SerializeField] private DialogueLine[] levelIntroDialogue;
        [Tooltip("Lo que dice el instructor al terminar el nivel sin morir (ganando o rindiéndose), antes del resultado.")]
        [SerializeField] private DialogueLine[] levelOutroDialogue;

        [Header("Despues del combate")]
        [Tooltip("Escena a la que se pasa unos segundos despues de terminar el combate (Arena 1 -> Ludus, Arena 2 -> Menu).")]
        [SerializeField] private string nextSceneAfterCombat = "Menu";
        [SerializeField] private float delayBeforeNextScene = 2.5f;
        [Tooltip("Oleadas (Nivel 1): segundos entre matar a un gladiador (festeja el público) y pasar al siguiente combate.")]
        [SerializeField] private float delayBetweenWaves = 1f;
        [Tooltip("Oleadas (Nivel 1): segundos que se muestra el título de cada combate antes de que arranque.")]
        [SerializeField] private float waveTitleSeconds = 3f;
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
            // enemy puede ya ser null: al morir se hace Destroy, y segun el orden de Update
            // este chequeo puede correr recien el frame siguiente.
            if (!retargetedToSecondEnemy && enemy2 != null && player != null &&
                (enemy == null || enemy.IsDead) && !enemy2.IsDead)
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
                GladiatorRoster.ResetSlotToBasic(GladiatorRoster.SelectedIndex);
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

        /// Publica el resultado por EventManager y agenda el paso a la siguiente escena.
        private void EndCombat(CombatResult result)
        {
            if (result == CombatResult.Victory && grantsGladiatorUnlock)
                GladiatorRoster.UnlockNext();

            SoundManager.Play(result == CombatResult.Victory ? SoundId.CrowdCheer : SoundId.CrowdBoo);
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

            yield return PlayDialogue(levelIntroDialogue);
            yield return StartCoroutine(RunCountdown(activateCombatAfter: false));

            for (int i = 0; i < waves.Length; i++)
            {
                EnemyWave wave = waves[i];
                ShowAvailableOrders(wave.availableOrders);

                // Los dos quedan quietos en su punto de inicio hasta que arranca el combate: si no,
                // durante el cartel del rival / del tutorial ya caminaban uno hacia el otro.
                if (i > 0 && player != null)
                {
                    player.SetCombatEnabled(false);
                    player.TeleportTo(playerSpawnPoint.position);
                    player.FullyHeal();
                }

                enemy = enemyPool.Get(enemySpawnPoint.position, Quaternion.identity);
                enemy?.SetCombatEnabled(false);
                enemy?.ConfigureStats(wave.maxHP, wave.minDamage, wave.maxDamage, wave.attackCooldown);
                enemy?.SetInvulnerable(wave.invulnerable);

                if (player != null && enemy != null)
                {
                    player.SetTarget(enemy.transform);
                    enemy.SetTarget(player.transform);
                }

                yield return ShowWaveTitle(wave.label);
                yield return PlayDialogue(wave.introDialogue);

                if (wave.requiredIntentToStart != PlayerIntent.None)
                    yield return StartCoroutine(WaitForRequiredIntent(wave));

                SetGladiatorsFighting(true);
                SetPromptHighlight(wave.highlightDuringFight);
                yield return RunFight(wave);
                SetPromptHighlight(PlayerIntent.None);
                HideHint();

                if (player == null || player.IsDead)
                {
                    GladiatorRoster.ResetSlotToBasic(0);
                    EndCombat(CombatResult.Defeat);
                    yield break;
                }

                if (player.HasSurrendered)
                {
                    yield return PlayDialogue(levelOutroDialogue);
                    EndCombat(CombatResult.Surrender);
                    yield break;
                }

                // Entre combates: festeja el público, el cuerpo queda en la arena durante el respiro
                // (se ve la animación Dead) y recién después vuelve al pool. Después del último no hace
                // falta: EndCombat ya hace festejar al público y se cambia de escena.
                if (i < waves.Length - 1)
                {
                    SoundManager.Play(SoundId.CrowdCheer);
                    yield return new WaitForSeconds(delayBetweenWaves);
                    enemyPool.Release(enemy);
                }
            }

            yield return PlayDialogue(levelOutroDialogue);
            EndCombat(CombatResult.Victory);
        }

        /// Espera a que termine el combate (muere alguno o el jugador se rinde). Si la oleada tiene
        /// diálogo de mitad de pelea, a los midFightDelaySeconds pausa a los dos, habla el instructor
        /// y queda el cartel de ayuda hasta el final.
        private IEnumerator RunFight(EnemyWave wave)
        {
            bool hasMidFightDialogue = wave.midFightDialogue != null && wave.midFightDialogue.Length > 0;
            float fightTime = 0f;

            while (IsFightOngoing())
            {
                if (hasMidFightDialogue && fightTime >= wave.midFightDelaySeconds)
                {
                    hasMidFightDialogue = false;
                    SetGladiatorsFighting(false);
                    yield return PlayDialogue(wave.midFightDialogue);
                    ShowHint(wave.midFightHint);
                    SetGladiatorsFighting(true);
                }

                fightTime += Time.deltaTime;
                yield return null;
            }
        }

        private bool IsFightOngoing()
            => enemy != null && !enemy.IsDead && player != null && !player.IsDead && !player.HasSurrendered;

        private void SetGladiatorsFighting(bool fighting)
        {
            player?.SetCombatEnabled(fighting);
            enemy?.SetCombatEnabled(fighting);
        }

        // Qué bit de OrderSet corresponde a cada posición de orderButtons (Atacá, Defiéndete, Ríndete).
        private static readonly OrderSet[] OrderButtonFlags = { OrderSet.Attack, OrderSet.Defend, OrderSet.Surrender };

        /// Muestra solo los botones de orden habilitados para este combate (Nothing = todos).
        /// OrderSet es un enum de "flags": cada botón es un bit, y con el operador & preguntamos si ese
        /// bit está prendido en la combinación elegida en el Inspector.
        private void ShowAvailableOrders(OrderSet available)
        {
            for (int i = 0; i < orderButtons.Length && i < OrderButtonFlags.Length; i++)
            {
                if (orderButtons[i] != null)
                    orderButtons[i].SetActive(available == 0 || (available & OrderButtonFlags[i]) != 0);
            }
        }

        private void SetPromptHighlight(PlayerIntent intent)
        {
            if (dialogue != null)
                dialogue.SetPromptHighlight(intent);
        }

        private IEnumerator PlayDialogue(DialogueLine[] lines)
        {
            if (dialogue != null && lines != null && lines.Length > 0)
                yield return dialogue.Play(lines);
        }

        private void ShowHint(string text)
        {
            if (tutorialHintLabel == null || string.IsNullOrEmpty(text)) return;
            tutorialHintLabel.text = text;
            tutorialHintLabel.gameObject.SetActive(true);
        }

        private void HideHint()
        {
            if (tutorialHintLabel != null)
                tutorialHintLabel.gameObject.SetActive(false);
        }

        /// Muestra el nombre del combate ("Enemigo 2: El Agresivo") con los dos gladiadores quietos.
        private IEnumerator ShowWaveTitle(string title)
        {
            if (countdownLabel == null || string.IsNullOrEmpty(title)) yield break;

            countdownLabel.gameObject.SetActive(true);
            countdownLabel.text = title;
            yield return new WaitForSeconds(waveTitleSeconds);
            countdownLabel.gameObject.SetActive(false);
        }

        /// Bloquea el arranque del combate hasta que el jugador toque el botón pedido, que queda resaltado
        /// mientras tanto. El cartel de ayuda es opcional (tutorialHint vacío = sin cartel).
        private IEnumerator WaitForRequiredIntent(EnemyWave wave)
        {
            ShowHint(wave.tutorialHint);
            SetPromptHighlight(wave.requiredIntentToStart);

            while (intentController == null || intentController.CurrentIntent != wave.requiredIntentToStart)
                yield return null;

            intentController.ClearIntent();
            SetPromptHighlight(PlayerIntent.None);
            HideHint();
        }
    }
}
