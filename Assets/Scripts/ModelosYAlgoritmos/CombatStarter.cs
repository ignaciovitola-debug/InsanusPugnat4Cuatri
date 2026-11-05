using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    // Maneja el flujo de un combate: crea a los gladiadores, las oleadas y el resultado.
    // El tutorial y los puntos de aparicion los resuelven TutorialFlow y SpawnPicker.
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
            [Tooltip("Tipo de rival (Flyweight). Vacio en el tutorial.")]
            public EnemyArchetype archetype;

            [Header("Tutorial (opcional)")]
            [Tooltip("Botones de orden visibles en este combate. Nothing = todos.")]
            public OrderSet availableOrders;
            [Tooltip("Lo que dice el instructor antes de que arranque el combate.")]
            public DialogueLine[] introDialogue;
            [Tooltip("Si no es None, el combate espera a que toquen ese boton.")]
            public PlayerIntent requiredIntentToStart;
            [Tooltip("Cartel que se ve mientras se espera ese boton.")]
            [TextArea] public string tutorialHint;
            public bool invulnerable;
            [Tooltip("Boton resaltado durante toda la pelea.")]
            public PlayerIntent highlightDuringFight;
            [Tooltip("Lo que dice el instructor en medio del combate (pausa la pelea).")]
            public DialogueLine[] midFightDialogue;
            public float midFightDelaySeconds;
            [Tooltip("Cartel que queda despues del dialogo de mitad de pelea.")]
            [TextArea] public string midFightHint;
        }

        [Header("Factory")]
        [SerializeField] private GladiatorFactory factory;

        [Header("Puntos de spawn")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform enemySpawnPoint2;

        [Tooltip("Cuanto se corre el lugar de aparicion de cada punto (X costados, Z fondo).")]
        [SerializeField] private Vector2 spawnJitter = new Vector2(1.5f, 3f);

        [Header("Oleadas secuenciales (opcional, Nivel 1)")]
        [Tooltip("Si tiene elementos, los enemigos salen uno tras otro con estas stats.")]
        [SerializeField] private EnemyWave[] waves;

        [Header("Cuenta regresiva")]
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private TMP_Text countdownLabel;
        [Tooltip("Cartel aparte para los mensajes del tutorial.")]
        [SerializeField] private TMP_Text tutorialHintLabel;

        [Header("Consignas del jugador")]
        [SerializeField] private PlayerIntentController intentController;

        [Header("Tutorial (opcional, Nivel 1)")]
        [SerializeField] private TutorialDialogue dialogue;
        [Tooltip("En este orden: Ataca, Defiende, Rindete.")]
        [SerializeField] private GameObject[] orderButtons = new GameObject[0];
        [Tooltip("Lo que dice el instructor al entrar a la arena.")]
        [SerializeField] private DialogueLine[] levelIntroDialogue;
        [Tooltip("Lo que dice el instructor al terminar el nivel sin morir.")]
        [SerializeField] private DialogueLine[] levelOutroDialogue;

        [Header("Despues del combate")]
        [Tooltip("Escena que se carga unos segundos despues de terminar el combate.")]
        [SerializeField] private string nextSceneAfterCombat = "Menu";
        [SerializeField] private float delayBeforeNextScene = 2.5f;
        [Tooltip("Segundos entre un combate y el siguiente.")]
        [SerializeField] private float delayBetweenWaves = 1f;
        [Tooltip("Segundos que se muestra el titulo de cada combate.")]
        [SerializeField] private float waveTitleSeconds = 3f;
        [Tooltip("Solo en la Arena 2: ganar desbloquea al siguiente gladiador.")]
        [SerializeField] private bool grantsGladiatorUnlock;
        [Tooltip("Denarios que se ganan al vencer (0 = ninguno).")]
        [SerializeField] private int victoryReward;

        private GladiatorNPC player;
        private GladiatorNPC enemy;
        private GladiatorNPC enemy2;
        private bool singleEncounterEnded;
        private bool fightResolved;
        private GladiatorPool enemyPool;
        private SpawnPicker spawnPicker;
        private TutorialFlow tutorial;

        // True si el combate viene de un contrato elegido en la Ludus.
        private bool contractMode;

        private bool IsWaveMode => waves != null && waves.Length > 0;

        private void OnEnable()
        {
            EventManager.Subscribe<GladiatorDiedEvent>(OnGladiatorDied);
            EventManager.Subscribe<GladiatorSurrenderedEvent>(OnGladiatorSurrendered);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<GladiatorDiedEvent>(OnGladiatorDied);
            EventManager.Unsubscribe<GladiatorSurrenderedEvent>(OnGladiatorSurrendered);
        }

        private void Start()
        {
            enemyPool = new GladiatorPool(factory, GladiatorKind.Enemy);
            spawnPicker = new SpawnPicker(spawnJitter);
            tutorial = new TutorialFlow(dialogue, tutorialHintLabel, orderButtons, intentController);

            if (!IsWaveMode && CombatSetup.Current != null)
                BuildWavesFromContract(CombatSetup.Current);

            if (IsWaveMode)
                StartCoroutine(RunWaveSequence());
            else
                StartSingleEncounter();
        }

        // Arma una oleada por rival: arquetipo al azar y stats que decide la dificultad (Strategy).
        private void BuildWavesFromContract(CombatContract contract)
        {
            waves = new EnemyWave[contract.enemyCount];
            EnemyArchetype previous = null;

            for (int i = 0; i < waves.Length; i++)
            {
                var archetype = PickArchetype(previous);
                previous = archetype;

                var stats = contract.difficulty.Scale(archetype.BaseStats, contract.levelMultiplier);
                string title = contract.enemyCount == 1 ? contract.difficulty.DisplayName : $"Rival {i + 1} de {contract.enemyCount}";

                waves[i] = new EnemyWave
                {
                    label = $"{title}: {archetype.DisplayName}",
                    archetype = archetype,
                    maxHP = stats.maxHP,
                    minDamage = stats.minDamage,
                    maxDamage = stats.maxDamage,
                    attackCooldown = stats.attackCooldown,
                };
            }
            contractMode = true;
        }

        // Elige un arquetipo al azar, sin repetir el del rival anterior.
        private static EnemyArchetype PickArchetype(EnemyArchetype previous)
        {
            var all = EconomyConfig.Instance.archetypes;
            if (all.Length == 0)
            {
                Debug.LogWarning("EconomyConfig no tiene arquetipos: se usa uno por defecto.");
                return ScriptableObject.CreateInstance<EnemyArchetype>();
            }

            var pick = all[Random.Range(0, all.Length)];
            if (pick == previous && all.Length > 1)
                pick = all[(System.Array.IndexOf(all, pick) + 1) % all.Length];
            return pick;
        }

        // El aspecto sale del arquetipo; si es igual al del jugador o no tiene, se sortea otro.
        private static int ChooseEnemySkin(EnemyWave wave)
        {
            int playerSkin = GladiatorRoster.GetSelected().skin;
            bool usable = wave.archetype != null && wave.archetype.SkinIndex >= 0 && wave.archetype.SkinIndex != playerSkin;
            return usable ? wave.archetype.SkinIndex : GladiatorSkins.RandomIndex(playerSkin);
        }

        private void StartSingleEncounter()
        {
            var selected = GladiatorRoster.GetSelected();
            player = factory.Create(GladiatorKind.Player, spawnPicker.Pick(playerSpawnPoint), Quaternion.identity);
            if (player != null)
            {
                player.ConfigureStats(selected.maxHP, selected.minDamage, selected.maxDamage, selected.attackCooldown);
                player.ApplySkin(selected.skin);
            }

            enemy = enemyPool.Get(spawnPicker.Pick(enemySpawnPoint), Quaternion.identity);
            enemy?.ApplySkin(GladiatorSkins.RandomIndex(selected.skin));

            if (player != null && enemy != null)
            {
                player.SetTarget(enemy.transform);
                enemy.SetTarget(player.transform);
            }

            if (enemySpawnPoint2 != null)
            {
                enemy2 = enemyPool.Get(spawnPicker.Pick(enemySpawnPoint2), Quaternion.identity);
                enemy2?.ApplySkin(GladiatorSkins.RandomIndex(selected.skin));
                if (player != null && enemy2 != null)
                    enemy2.SetTarget(player.transform);
                enemy2?.SetCombatEnabled(false);
            }

            player?.SetIntentController(intentController);

            player?.SetCombatEnabled(false);
            enemy?.SetCombatEnabled(false);

            StartCoroutine(RunCountdown(activateCombatAfter: true));
        }

        private void OnGladiatorDied(GladiatorDiedEvent e)
        {
            GladiatorNPC fallen = e.gladiator;

            if (IsWaveMode)
            {
                if (fallen == player || fallen == enemy)
                    fightResolved = true;
                return;
            }

            // Cuando el primer enemigo cae, el jugador pasa a enfrentar al segundo.
            if (fallen == enemy && enemy2 != null && !enemy2.IsDead && player != null)
                player.SetTarget(enemy2.transform);

            if (fallen == enemy || fallen == enemy2)
                StartCoroutine(RetireEnemy(fallen, fallen.CorpseLifetime));

            EvaluateSingleEncounter();
        }

        private void OnGladiatorSurrendered(GladiatorSurrenderedEvent e)
        {
            if (e.gladiator != player) return;

            if (IsWaveMode)
                fightResolved = true;
            else
                EvaluateSingleEncounter();
        }

        // Se revisa cuando llega un evento de muerte o rendicion, no cada frame.
        private void EvaluateSingleEncounter()
        {
            if (singleEncounterEnded || player == null) return;

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

        // Publica el resultado por EventManager y agenda el cambio de escena.
        private void EndCombat(CombatResult result)
        {
            bool won = result == CombatResult.Victory;

            // Las oleadas sin contrato son el tutorial: al terminarlo no se vuelve a mostrar.
            if (IsWaveMode && !contractMode)
                GladiatorRoster.ArenaTutorialDone = true;

            if (won && grantsGladiatorUnlock)
                GladiatorRoster.UnlockNext();

            int reward = won ? (contractMode ? CombatSetup.Current.reward : victoryReward) : 0;
            Denarii.Add(reward);
            CombatSetup.Current = null;

            SoundManager.Play(won ? SoundId.CrowdCheer : SoundId.CrowdBoo);
            GameSettings.Vibrate();
            EventManager.Raise(new CombatEndedEvent(result, reward));
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
            player = factory.Create(GladiatorKind.Player, spawnPicker.Pick(playerSpawnPoint), Quaternion.identity);
            player?.ApplySkin(GladiatorRoster.GetSelected().skin);
            if (contractMode)
            {
                var selected = GladiatorRoster.GetSelected();
                player?.ConfigureStats(selected.maxHP, selected.minDamage, selected.maxDamage, selected.attackCooldown);
            }
            player?.SetIntentController(intentController);
            player?.SetCombatEnabled(false);

            yield return tutorial.PlayDialogue(levelIntroDialogue);
            yield return StartCoroutine(RunCountdown(activateCombatAfter: false));

            for (int i = 0; i < waves.Length; i++)
            {
                EnemyWave wave = waves[i];
                tutorial.ShowAvailableOrders(wave.availableOrders);

                // Los dos esperan quietos en su punto hasta que arranca el combate.
                if (i > 0 && player != null)
                {
                    player.SetCombatEnabled(false);
                    player.TeleportTo(spawnPicker.Pick(playerSpawnPoint));
                    // En el tutorial se cura entre combates; en los contratos no.
                    if (!contractMode)
                        player.FullyHeal();
                }

                enemy = enemyPool.Get(spawnPicker.Pick(enemySpawnPoint), Quaternion.identity);
                enemy?.ApplySkin(ChooseEnemySkin(wave));
                enemy?.SetCombatEnabled(false);
                enemy?.ApplyArchetype(wave.archetype, new CombatStats(wave.maxHP, wave.minDamage, wave.maxDamage, wave.attackCooldown));
                enemy?.SetInvulnerable(wave.invulnerable);

                if (player != null && enemy != null)
                {
                    player.SetTarget(enemy.transform);
                    enemy.SetTarget(player.transform);
                }

                yield return ShowWaveTitle(wave.label);
                yield return tutorial.PlayDialogue(wave.introDialogue);

                if (wave.requiredIntentToStart != PlayerIntent.None)
                    yield return StartCoroutine(tutorial.WaitForRequiredIntent(wave.requiredIntentToStart, wave.tutorialHint));

                fightResolved = false;
                SetGladiatorsFighting(true);
                tutorial.SetPromptHighlight(wave.highlightDuringFight);
                yield return RunFight(wave);
                tutorial.SetPromptHighlight(PlayerIntent.None);
                tutorial.HideHint();

                if (player == null || player.IsDead)
                {
                    GladiatorRoster.ResetSlotToBasic(contractMode ? GladiatorRoster.SelectedIndex : 0);
                    EndCombat(CombatResult.Defeat);
                    yield break;
                }

                if (player.HasSurrendered)
                {
                    yield return tutorial.PlayDialogue(levelOutroDialogue);
                    EndCombat(CombatResult.Surrender);
                    yield break;
                }

                // Entre combates el publico festeja y el cuerpo queda un rato antes de volver al pool.
                if (i < waves.Length - 1)
                {
                    SoundManager.Play(SoundId.CrowdCheer);
                    yield return RetireEnemy(enemy, delayBetweenWaves);
                }
            }

            yield return tutorial.PlayDialogue(levelOutroDialogue);
            EndCombat(CombatResult.Victory);
        }

        // Espera a que muera alguno o el jugador se rinda. Si hay dialogo de mitad de pelea, pausa y lo muestra.
        private IEnumerator RunFight(EnemyWave wave)
        {
            bool hasMidFightDialogue = wave.midFightDialogue != null && wave.midFightDialogue.Length > 0;
            float fightTime = 0f;

            while (!fightResolved && enemy != null && player != null)
            {
                if (hasMidFightDialogue && fightTime >= wave.midFightDelaySeconds)
                {
                    hasMidFightDialogue = false;
                    SetGladiatorsFighting(false);
                    yield return tutorial.PlayDialogue(wave.midFightDialogue);
                    tutorial.ShowHint(wave.midFightHint);
                    SetGladiatorsFighting(true);
                }

                fightTime += Time.deltaTime;
                yield return null;
            }
        }

        // Deja el cuerpo un rato en la arena y despues lo devuelve al pool.
        private IEnumerator RetireEnemy(GladiatorNPC fallen, float delay)
        {
            yield return new WaitForSeconds(delay);
            enemyPool.Release(fallen);
        }

        private void SetGladiatorsFighting(bool fighting)
        {
            player?.SetCombatEnabled(fighting);
            enemy?.SetCombatEnabled(fighting);
        }

        // Muestra el nombre del combate con los dos gladiadores quietos.
        private IEnumerator ShowWaveTitle(string title)
        {
            if (countdownLabel == null || string.IsNullOrEmpty(title)) yield break;

            countdownLabel.gameObject.SetActive(true);
            countdownLabel.text = title;
            yield return new WaitForSeconds(waveTitleSeconds);
            countdownLabel.gameObject.SetActive(false);
        }
    }
}
