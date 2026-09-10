using UnityEngine;
using System.Collections;
using TMPro;

namespace GladiusAI
{
    public class CombatStarter : MonoBehaviour
    {
        [Header("Factory")]
        [SerializeField] private GladiatorFactory factory;

        [Header("Puntos de spawn")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform enemySpawnPoint;
        [SerializeField] private Transform enemySpawnPoint2; // opcional: si se asigna, spawnea un segundo enemigo (Nivel 2)

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
    }
}
