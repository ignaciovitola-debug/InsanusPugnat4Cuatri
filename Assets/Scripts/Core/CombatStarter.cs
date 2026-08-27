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

        [Header("Cuenta regresiva")]
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private TMP_Text countdownLabel;

        [Header("Consignas del jugador")]
        [SerializeField] private PlayerIntentController intentController;

        private void Start()
        {
            var player = factory.CreatePlayer(playerSpawnPoint.position, Quaternion.identity);
            var enemy = factory.CreateEnemy(enemySpawnPoint.position, Quaternion.identity);

            if (player != null && enemy != null)
            {
                player.SetTarget(enemy.transform);
                enemy.SetTarget(player.transform);
            }

            player?.SetIntentController(intentController);

            player?.SetCombatEnabled(false);
            enemy?.SetCombatEnabled(false);

            StartCoroutine(CountdownAndBegin(player, enemy));
        }

        private IEnumerator CountdownAndBegin(GladiatorNPC player, GladiatorNPC enemy)
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

            yield return new WaitForSeconds(1f);

            if (countdownLabel != null)
                countdownLabel.gameObject.SetActive(false);
        }
    }
}
