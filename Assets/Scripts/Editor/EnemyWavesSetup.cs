using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Configura las 3 oleadas secuenciales de ArenaDeCombate1 (Novato ->
    /// Agresivo -> Veterano), tal como pide el profesor: que la documentación
    /// coincida con lo que realmente pasa en el juego. No afecta a
    /// ArenaDeCombate2, que usa un campo distinto y opcional.
    /// </summary>
    public static class EnemyWavesSetup
    {
        [MenuItem("Tools/Insanus Pugnat/Configurar Oleadas - Nivel 1 (ArenaDeCombate1)")]
        public static void ConfigureWaves()
        {
            var combatStarter = Object.FindFirstObjectByType<CombatStarter>();
            if (combatStarter == null)
            {
                Debug.LogError("[EnemyWavesSetup] No encontré 'CombatStarter' en la escena activa. Abrí ArenaDeCombate1 y volvé a intentar.");
                return;
            }

            var so = new SerializedObject(combatStarter);
            var wavesProp = so.FindProperty("waves");
            if (wavesProp == null)
            {
                Debug.LogError("[EnemyWavesSetup] CombatStarter no tiene el campo 'waves'. Verificá que CombatStarter.cs esté actualizado.");
                return;
            }

            wavesProp.arraySize = 3;

            SetWave(wavesProp.GetArrayElementAtIndex(0), "Enemigo 1: El Novato", 60f, 5f, 10f, 2f);
            SetWave(wavesProp.GetArrayElementAtIndex(1), "Enemigo 2: El Agresivo", 100f, 10f, 20f, 1.2f);
            SetWave(wavesProp.GetArrayElementAtIndex(2), "Enemigo 3: El Veterano", 160f, 15f, 25f, 1f);

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[EnemyWavesSetup] 3 oleadas configuradas: Novato -> Agresivo -> Veterano.");
        }

        private static void SetWave(SerializedProperty waveProp, string label, float maxHP, float minDmg, float maxDmg, float cooldown)
        {
            waveProp.FindPropertyRelative("label").stringValue = label;
            waveProp.FindPropertyRelative("maxHP").floatValue = maxHP;
            waveProp.FindPropertyRelative("minDamage").floatValue = minDmg;
            waveProp.FindPropertyRelative("maxDamage").floatValue = maxDmg;
            waveProp.FindPropertyRelative("attackCooldown").floatValue = cooldown;
        }
    }
}
