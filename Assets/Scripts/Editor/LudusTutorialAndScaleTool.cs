using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso: (1) achica 10% los dos prefabs de gladiador y (2) agrega a la Ludus el cuadro
    // del instructor (copia del de Arena 1) con el tutorial de la primera visita.
    // Borrar este archivo despues de usarla.
    public static class LudusTutorialAndScaleTool
    {
        private const float GladiatorScale = 0.9f;
        private const string LudusPath = "Assets/Scenes/Ludus.unity";
        private const string ArenaPath = "Assets/Scenes/ArenaDeCombate1.unity";

        private static readonly string[] PrefabPaths =
        {
            "Assets/Art/Prefabs/Gladiator.prefab",
            "Assets/Art/Prefabs/GladiatorEnemy.prefab",
        };

        private struct Line
        {
            public PortraitExpression face;
            public string text;
            public Line(PortraitExpression face, string text) { this.face = face; this.text = text; }
        }

        private static readonly Line[] Lines =
        {
            new Line(PortraitExpression.Smile, "¡Bienvenido a la Ludus, campeón! Acá se entrenan los gladiadores que van a pelear por tu gloria."),
            new Line(PortraitExpression.Calm, "Tocá a un gladiador para elegirlo. El que queda resaltado en amarillo es el que va a salir a la arena."),
            new Line(PortraitExpression.Talk, "Cada uno tiene su propia vida, fuerza y velocidad de ataque. Fijate en los números de su cartel antes de decidir."),
            new Line(PortraitExpression.Calm, "Los que se ven oscuros todavía no son tuyos. Cada vez que ganes un combate en la Arena 2, se suma uno nuevo, con habilidades al azar."),
            new Line(PortraitExpression.Aggression, "Y ojo: si tu gladiador cae en combate, pierde su entrenamiento y vuelve a sus habilidades básicas."),
            new Line(PortraitExpression.Smile, "Cuando estés listo, tocá el botón para ir al combate. ¡Que el público grite tu nombre!"),
        };

        [MenuItem("Tools/Insanus Pugnat/Tutorial Ludus + achicar gladiadores")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ScalePrefabs();

            string original = SceneManager.GetActiveScene().path;
            try { AddTutorialToLudus(); }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        private static void ScalePrefabs()
        {
            foreach (string path in PrefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                root.transform.localScale = Vector3.one * GladiatorScale;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[{path}] escala {GladiatorScale}.");
            }
        }

        private static void AddTutorialToLudus()
        {
            var ludus = EditorSceneManager.OpenScene(LudusPath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<LudusController>(FindObjectsInactive.Include);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (controller == null || canvas == null)
            {
                Debug.LogError("[Ludus] falta LudusController o Canvas.");
                return;
            }

            if (Object.FindFirstObjectByType<TutorialDialogue>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("[Ludus] ya tiene un TutorialDialogue, no se agrega otro.");
                return;
            }

            var arena = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Additive);
            TutorialDialogue source = null;
            foreach (var d in Object.FindObjectsByType<TutorialDialogue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (d.gameObject.scene == arena) source = d;

            if (source == null)
            {
                Debug.LogError("[Ludus] no encontre el TutorialDialogue en ArenaDeCombate1.");
                EditorSceneManager.CloseScene(arena, true);
                return;
            }

            var clone = Object.Instantiate(source.gameObject);
            clone.name = "TutorialDialogue";
            SceneManager.MoveGameObjectToScene(clone, ludus);
            clone.transform.SetParent(canvas.transform, false);
            clone.transform.SetAsLastSibling();
            clone.SetActive(false);
            EditorSceneManager.CloseScene(arena, true);

            var dialogue = clone.GetComponent<TutorialDialogue>();
            var dialogueSo = new SerializedObject(dialogue);
            dialogueSo.FindProperty("orderButtons").arraySize = 0;
            dialogueSo.ApplyModifiedProperties();

            var so = new SerializedObject(controller);
            so.FindProperty("dialogue").objectReferenceValue = dialogue;
            var lines = so.FindProperty("introDialogue");
            lines.arraySize = Lines.Length;
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("expression").enumValueIndex = (int)Lines[i].face;
                line.FindPropertyRelative("talkWhileWriting").boolValue = true;
                line.FindPropertyRelative("appearAgain").boolValue = false;
                line.FindPropertyRelative("highlight").enumValueIndex = (int)DialogueHighlight.None;
                line.FindPropertyRelative("text").stringValue = Lines[i].text;
            }
            so.ApplyModifiedProperties();

            if (!EditorSceneManager.SaveScene(ludus))
                Debug.LogError("[Ludus] no se pudo guardar.");
            else
                Debug.Log("[Ludus] tutorial agregado.");
        }
    }
}
