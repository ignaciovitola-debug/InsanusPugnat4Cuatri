using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Agrega el segundo punto de spawn de enemigo en ArenaDeCombate2 y lo
    /// conecta al CombatStarter de la escena a través del campo opcional
    /// "enemySpawnPoint2" — si una escena no lo tiene asignado (como
    /// ArenaDeCombate1), sigue siendo 1v1 exactamente como hasta ahora.
    /// </summary>
    public static class SecondEnemySetup
    {
        [MenuItem("Tools/Insanus Pugnat/Agregar Segundo Enemigo - Nivel 2 (ArenaDeCombate2)")]
        public static void AddSecondEnemy()
        {
            var combatStarter = Object.FindFirstObjectByType<CombatStarter>();
            if (combatStarter == null)
            {
                Debug.LogError("[SecondEnemySetup] No encontré 'CombatStarter' en la escena activa. Abrí ArenaDeCombate2 y volvé a intentar.");
                return;
            }

            GameObject firstEnemySpawn = GameObject.Find("Spawn (1)");
            if (firstEnemySpawn == null)
            {
                Debug.LogError("[SecondEnemySetup] No encontré 'Spawn (1)' en la escena activa.");
                return;
            }

            Vector3 secondSpawnPos = firstEnemySpawn.transform.position + new Vector3(0f, 0f, 2.5f);

            var spawn2GO = new GameObject("Spawn (2)");
            spawn2GO.transform.position = secondSpawnPos;
            Undo.RegisterCreatedObjectUndo(spawn2GO, "Agregar Segundo Enemigo Nivel 2");

            var serializedStarter = new SerializedObject(combatStarter);
            var spawnProp = serializedStarter.FindProperty("enemySpawnPoint2");
            if (spawnProp == null)
            {
                Debug.LogError("[SecondEnemySetup] CombatStarter no tiene el campo 'enemySpawnPoint2'. Verificá que CombatStarter.cs esté actualizado.");
                return;
            }

            spawnProp.objectReferenceValue = spawn2GO.transform;
            serializedStarter.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[SecondEnemySetup] Segundo enemigo conectado. 'Spawn (2)' creado en {secondSpawnPos}. " +
                "Usa el mismo prefab de enemigo que el primero (asignado en GladiatorFactory).");
        }
    }
}
