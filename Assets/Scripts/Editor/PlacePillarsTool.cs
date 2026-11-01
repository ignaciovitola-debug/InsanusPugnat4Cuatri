using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: pone pilares ambientales de piedra sobre la linea del muro
    // (las piezas "Pared") en Arena 1, Arena 2 y Ludus. Solo agrega un objeto "PilaresAmbientales".
    // Borrar este archivo despues de usarla.
    public static class PlacePillarsTool
    {
        private const string PillarPrefab = "Assets/Prefabs/Props/isometric-miniature-dungeon/Angle/stoneColumn_S.prefab";
        private const string RootName = "PilaresAmbientales";
        private const float PillarScale = 0.8f;
        // Fraccion de la altura del sprite del muro (stoneWallColumnIn_S) que esta vacia debajo de su base visible.
        private const float WallBaseFraction = 0.2265625f;
        private static readonly Regex WallName = new Regex(@"^Pared( \(\d+\))?$");

        private static readonly string[] Scenes =
        {
            "Assets/Scenes/ArenaDeCombate1.unity",
            "Assets/Scenes/ArenaDeCombate2.unity",
            "Assets/Scenes/Ludus.unity",
        };

        [MenuItem("Tools/Insanus Pugnat/Pilares ambientales")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string original = SceneManager.GetActiveScene().path;
            try
            {
                foreach (string path in Scenes)
                    Place(path);
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        private static void Place(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // Si ya habia pilares se reemplazan: la herramienta puede correrse de nuevo sin duplicar nada.
            var existing = GameObject.Find(RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            // El prefab se carga DESPUES de abrir la escena (Unity descarta lo que nadie referencia al abrirla).
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PillarPrefab);
            if (prefab == null) { Debug.LogError($"[{scene.name}] no encuentro {PillarPrefab}."); return; }

            var walls = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.gameObject.scene == scene && WallName.IsMatch(t.name))
                .OrderBy(t => t.position.x)
                .ToArray();
            if (walls.Length < 2) { Debug.LogError($"[{scene.name}] no encontre el muro (piezas Pared)."); return; }

            // Se mide el sprite del muro (no la raiz del prefab, que esta corrida): su base visible queda a esa
            // fraccion de la altura total del sprite, igual que el pivote que se le puso al pilar.
            var sprites = walls.Select(w => w.GetComponentInChildren<SpriteRenderer>()).Where(s => s != null).OrderBy(s => s.bounds.center.x).ToArray();
            if (sprites.Length < 2) { Debug.LogError($"[{scene.name}] las piezas Pared no tienen SpriteRenderer."); return; }

            var first = sprites[0].bounds;
            float wallZ = sprites[0].transform.position.z;
            float baseY = first.min.y + first.size.y * WallBaseFraction;
            float spacing = sprites[1].bounds.center.x - first.center.x;
            Debug.Log($"[{scene.name}] muro: {sprites.Length} piezas, primer sprite centro {first.center}, tamano {first.size}, z {wallZ:F2}, separacion {spacing:F2}.");

            // Un pilar en cada junta del muro, salteando una (separados por dos piezas), y uno de cierre al final.
            var joints = sprites.Take(sprites.Length - 1).Select(s => s.bounds.center.x + spacing / 2f).ToArray();
            var xs = joints.Where((x, i) => i % 2 == 0).ToList();
            xs.Add(sprites[sprites.Length - 1].bounds.center.x + spacing / 2f);

            var root = new GameObject(RootName);
            for (int i = 0; i < xs.Count; i++)
            {
                var pillar = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                pillar.name = $"Pilar_{i + 1}";
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.position = new Vector3(xs[i], baseY, wallZ);
                pillar.transform.localScale = Vector3.one * PillarScale;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] {xs.Count} pilares sobre el muro (z {wallZ:F2}, base y {baseY:F2}).");
        }
    }
}
