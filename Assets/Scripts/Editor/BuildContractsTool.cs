using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: crea las dos zonas nuevas copiando la Arena 2, las agrega a la lista
    // de escenas del build (global y de cada Build Profile), y arma el panel "Combates" en la Ludus.
    // Borrar este archivo despues de usarla.
    public static class BuildContractsTool
    {
        private const string Arena2 = "Assets/Scenes/ArenaDeCombate2.unity";
        private const string LudusPath = "Assets/Scenes/Ludus.unity";
        private const string ProfilesFolder = "Assets/Settings/Build Profiles";

        private static readonly string[] NewZones =
        {
            "Assets/Scenes/ArenaZona_Catacumbas.unity",
            "Assets/Scenes/ArenaZona_Patio.unity",
        };

        [MenuItem("Tools/Insanus Pugnat/Paso 2: zonas y combates")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateZones();
            AddScenesToBuilds();

            string original = SceneManager.GetActiveScene().path;
            try { BuildLudusCombats(); }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- zonas ----------

        private static void CreateZones()
        {
            foreach (string path in NewZones)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) { Debug.Log($"[Zonas] {path} ya existe."); continue; }
                Debug.Log(AssetDatabase.CopyAsset(Arena2, path) ? $"[Zonas] creada {path}." : $"[Zonas] NO se pudo crear {path}.");
            }
            AssetDatabase.Refresh();
        }

        private static void AddScenesToBuilds()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in NewZones)
                if (scenes.All(s => s.path != path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Zonas] lista global: {scenes.Count} escenas.");

            foreach (string guid in AssetDatabase.FindAssets("", new[] { ProfilesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (profile == null || path.EndsWith(".meta")) continue;

                var so = new SerializedObject(profile);
                var list = so.FindProperty("m_Scenes");
                if (list == null || !list.isArray) continue;

                bool added = false;
                foreach (string zone in NewZones)
                {
                    bool present = false;
                    for (int i = 0; i < list.arraySize; i++)
                        present |= list.GetArrayElementAtIndex(i).FindPropertyRelative("m_path").stringValue == zone;
                    if (present) continue;

                    list.arraySize++;
                    var element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    element.FindPropertyRelative("m_enabled").boolValue = true;
                    element.FindPropertyRelative("m_path").stringValue = zone;
                    added = true;
                }

                if (added)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(profile);
                    Debug.Log($"[Zonas] {path}: escenas agregadas ({list.arraySize} en total).");
                }
            }
            AssetDatabase.SaveAssets();
        }

        // ---------- Ludus ----------

        private static void BuildLudusCombats()
        {
            var scene = EditorSceneManager.OpenScene(LudusPath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var controller = Object.FindFirstObjectByType<LudusController>(FindObjectsInactive.Include);
            if (canvas == null || controller == null) { Debug.LogError("[Ludus] falta Canvas o LudusController."); return; }

            var goButton = canvas.transform.Find("IrAlCombateButton");
            if (goButton == null) { Debug.LogError("[Ludus] no hay IrAlCombateButton."); return; }

            if (canvas.transform.Find("Panel_Combates") != null) { Debug.Log("[Ludus] ya tenia el panel de combates."); return; }

            var panel = NewRect("Panel_Combates", canvas.transform, Vector2.zero, Vector2.one);
            panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.04f, 0.96f);
            NewText("Titulo", panel, "Elegí tu combate", 72, TextAlignmentOptions.Center, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.98f));

            var content = NewRect("Content", panel, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.82f));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 24f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var template = BuildRowTemplate(content);

            var back = Object.Instantiate(goButton.gameObject, panel);
            back.name = "Boton_Volver";
            back.GetComponentInChildren<TMP_Text>().text = "Volver";
            ClearAndBind(back.GetComponent<Button>(), controller.HideCombats);

            // El boton central de la Ludus pasa de "ir directo a la Arena 2" a "abrir la lista de combates".
            goButton.GetComponentInChildren<TMP_Text>().text = "Combates";
            ClearAndBind(goButton.GetComponent<Button>(), controller.ShowCombats);

            var so = new SerializedObject(controller);
            so.FindProperty("combatsPanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("contractContent").objectReferenceValue = content;
            so.FindProperty("contractRowTemplate").objectReferenceValue = template;
            so.ApplyModifiedProperties();

            // El cuadro del instructor y el boton "?" siguen dibujandose por encima.
            var dialogue = canvas.GetComponentInChildren<TutorialDialogue>(true);
            if (dialogue != null)
                panel.SetSiblingIndex(dialogue.transform.GetSiblingIndex());

            panel.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Ludus] panel de combates armado.");
        }

        private static ContractRow BuildRowTemplate(RectTransform content)
        {
            var row = NewRect("FilaContrato", content, Vector2.zero, Vector2.one);
            row.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.13f, 0.1f, 1f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 200f;

            var title = NewText("Titulo", row, "Dificultad", 54, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.5f), new Vector2(0.6f, 0.95f));
            title.fontStyle = FontStyles.Bold;
            var detail = NewText("Detalle", row, "Rivales y zona", 38, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.05f), new Vector2(0.6f, 0.5f));
            detail.color = new Color(0.85f, 0.8f, 0.7f);
            var reward = NewText("Premio", row, "0 denarios", 44, TextAlignmentOptions.Right, new Vector2(0.55f, 0.55f), new Vector2(0.97f, 0.95f));
            reward.color = new Color(1f, 0.85f, 0.3f);

            var buttonRect = NewRect("Boton_Combatir", row, new Vector2(0.65f, 0.08f), new Vector2(0.97f, 0.52f));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.15f, 1f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            NewText("Label", buttonRect, "Combatir", 44, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            var contractRow = row.gameObject.AddComponent<ContractRow>();
            var so = new SerializedObject(contractRow);
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("detailLabel").objectReferenceValue = detail;
            so.FindProperty("rewardLabel").objectReferenceValue = reward;
            so.FindProperty("fightButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();
            return contractRow;
        }

        // ---------- helpers ----------

        private static void ClearAndBind(Button button, UnityEngine.Events.UnityAction action)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = NewRect(name, parent, anchorMin, anchorMax);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
