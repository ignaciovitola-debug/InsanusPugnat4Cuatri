using UnityEditor;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Completa el blocking de ArenaDeCombate2 según el boceto del PDF: dos
    /// columnas simétricas formando un "portal" en el medio de la arena
    /// (a diferencia de Nivel 1, que las tiene en diagonal en las esquinas),
    /// para que la arena de Nivel 2 se sienta táctica y distinta.
    /// No toca piso, botones, cámara ni lógica de combate.
    /// </summary>
    public static class BlockingNivel2Builder
    {
        [MenuItem("Tools/Insanus Pugnat/Agregar Columnas - Nivel 2 (ArenaDeCombate2)")]
        public static void AddColumns()
        {
            GameObject playerSpawn = GameObject.Find("Spawn");
            GameObject enemySpawn = GameObject.Find("Spawn (1)");

            if (playerSpawn == null || enemySpawn == null)
            {
                Debug.LogError("[BlockingNivel2Builder] No encontré 'Spawn' y/o 'Spawn (1)' en la escena activa. " +
                    "Abrí ArenaDeCombate2 y volvé a intentar.");
                return;
            }

            Vector3 playerPos = playerSpawn.transform.position;
            Vector3 enemyPos = enemySpawn.transform.position;
            Vector3 mid = (playerPos + enemyPos) * 0.5f;
            float gateOffset = Vector3.Distance(playerPos, enemyPos) * 0.2f;

            GameObject parent = new GameObject("Blocking_Columnas_Nivel2");
            Undo.RegisterCreatedObjectUndo(parent, "Agregar Columnas Nivel 2");

            // Portal simétrico en el medio: una columna a cada lado del camino directo entre spawns.
            CreateColumn(parent.transform, mid + new Vector3(0f, 0f, gateOffset), "Columna_Norte");
            CreateColumn(parent.transform, mid + new Vector3(0f, 0f, -gateOffset), "Columna_Sur");

            Selection.activeGameObject = parent;
            Debug.Log($"[BlockingNivel2Builder] Columnas creadas formando un portal en el medio. " +
                $"Player Spawn: {playerPos} | Enemy Spawn: {enemyPos}. Ajustá posición/tamaño a gusto.");
        }

        private static void CreateColumn(Transform parent, Vector3 basePosition, string name)
        {
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent, false);
            column.transform.localScale = new Vector3(1f, 1.5f, 1f);
            column.transform.position = basePosition + new Vector3(0f, 1.5f, 0f); // apoyada sobre el piso

            Undo.RegisterCreatedObjectUndo(column, "Agregar Columnas Nivel 2");
        }
    }
}
