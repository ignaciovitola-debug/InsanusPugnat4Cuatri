using UnityEditor;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Herramienta de un solo uso para completar el blocking de ArenaDeCombate1
    /// según el boceto del PDF: agrega las dos columnas/obstáculo en diagonal
    /// (una cerca del jugador, una cerca del enemigo), posicionadas en relación
    /// a los GameObjects "Spawn" y "Spawn (1)" que ya existen en la escena —
    /// no toca piso, botones, cámara ni lógica de combate, que ya están armados.
    /// </summary>
    public static class BlockingNivel1Builder
    {
        [MenuItem("Tools/Insanus Pugnat/Agregar Columnas - Nivel 1 (ArenaDeCombate1)")]
        public static void AddColumns()
        {
            GameObject playerSpawn = GameObject.Find("Spawn");
            GameObject enemySpawn = GameObject.Find("Spawn (1)");

            if (playerSpawn == null || enemySpawn == null)
            {
                Debug.LogError("[BlockingNivel1Builder] No encontré 'Spawn' y/o 'Spawn (1)' en la escena activa. " +
                    "Abrí ArenaDeCombate1 y volvé a intentar.");
                return;
            }

            Vector3 playerPos = playerSpawn.transform.position;
            Vector3 enemyPos = enemySpawn.transform.position;
            Vector3 mid = (playerPos + enemyPos) * 0.5f;
            float sideOffset = Vector3.Distance(playerPos, enemyPos) * 0.25f;

            GameObject parent = new GameObject("Blocking_Columnas_Nivel1");
            Undo.RegisterCreatedObjectUndo(parent, "Agregar Columnas Nivel 1");

            // Columna cerca del jugador, corrida hacia el fondo de la arena (boceto: arriba-izquierda).
            CreateColumn(parent.transform, mid + new Vector3(-sideOffset, 0f, 3f), "Columna_LadoJugador");

            // Columna cerca del enemigo, corrida hacia el frente de la arena (boceto: abajo-derecha).
            CreateColumn(parent.transform, mid + new Vector3(sideOffset, 0f, -3f), "Columna_LadoEnemigo");

            Selection.activeGameObject = parent;
            Debug.Log($"[BlockingNivel1Builder] Columnas creadas. Player Spawn: {playerPos} | Enemy Spawn: {enemyPos}. " +
                "Ajustá posición y tamaño a gusto desde el Inspector.");
        }

        private static void CreateColumn(Transform parent, Vector3 basePosition, string name)
        {
            GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent, false);
            column.transform.localScale = new Vector3(1f, 1.5f, 1f);
            column.transform.position = basePosition + new Vector3(0f, 1.5f, 0f); // apoyada sobre el piso

            Undo.RegisterCreatedObjectUndo(column, "Agregar Columnas Nivel 1");
        }
    }
}
