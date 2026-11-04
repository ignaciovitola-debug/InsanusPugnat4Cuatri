using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: arma el prefab PanelOpciones (sliders, interruptores y calidad),
    // reemplaza el panel de opciones del Menu y agrega boton + menu de pausa a las cuatro arenas.
    // Borrar este archivo despues de usarla.
    public static class BuildOptionsTool
    {
        private const string PrefabFolder = "Assets/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/PanelOpciones.prefab";
        private const string MenuPath = "Assets/Scenes/Menu.unity";

        private static readonly (string path, string exitScene)[] Arenas =
        {
            ("Assets/Scenes/ArenaDeCombate1.unity", "Menu"),
            ("Assets/Scenes/ArenaDeCombate2.unity", "Ludus"),
            ("Assets/Scenes/ArenaZona_Catacumbas.unity", "Ludus"),
            ("Assets/Scenes/ArenaZona_Patio.unity", "Ludus"),
        };

        private static readonly Color PanelColor = new Color(0.07f, 0.05f, 0.04f, 0.97f);
        private static readonly Color ButtonColor = new Color(0.6f, 0.2f, 0.15f, 1f);

        [MenuItem("Tools/Insanus Pugnat/Paso 4: opciones y pausa")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateOptionsPrefab();

            string original = SceneManager.GetActiveScene().path;
            try
            {
                BuildMenu();
                foreach (var arena in Arenas)
                    BuildPause(arena.path, arena.exitScene);
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- prefab del panel de opciones ----------

        private static void CreateOptionsPrefab()
        {
            if (File.Exists(PrefabPath)) return;
            if (!Directory.Exists(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            var root = NewRect("PanelOpciones", null, Vector2.zero, Vector2.one);
            root.gameObject.AddComponent<Image>().color = PanelColor;
            var controller = root.gameObject.AddComponent<OptionsController>();

            NewText("Titulo", root, "Opciones", 72, TextAlignmentOptions.Center, new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f));

            var scrollRect = NewRect("Scroll View", root, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.86f));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            var viewport = NewRect("Viewport", scrollRect, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var master = SliderRow(content, "Volumen general");
            var music = SliderRow(content, "Música");
            var sfx = SliderRow(content, "Efectos");
            var brightness = SliderRow(content, "Brillo");
            var vibration = ToggleRow(content, "Vibración");
            var fps = ToggleRow(content, "60 FPS (apagado: 30, ahorra batería)");
            var qualityLabel = QualityRow(content, controller);

            var wipeRow = Row(content, "Progreso");
            wipeRow.name = "FilaBorrarDatos";
            var wipe = MakeButton("Boton_BorrarDatos", wipeRow, "Borrar datos", 40, 560f, 100f);
            wipe.gameObject.AddComponent<WipeDataButton>();
            var wipeSo = new SerializedObject(wipe.GetComponent<WipeDataButton>());
            wipeSo.FindProperty("label").objectReferenceValue = wipe.GetComponentInChildren<TMP_Text>();
            wipeSo.ApplyModifiedProperties();

            var back = MakeButton("Boton_Volver", root, "Volver", 48, 500f, 110f);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 40f);

            var so = new SerializedObject(controller);
            so.FindProperty("masterSlider").objectReferenceValue = master;
            so.FindProperty("musicSlider").objectReferenceValue = music;
            so.FindProperty("sfxSlider").objectReferenceValue = sfx;
            so.FindProperty("brightnessSlider").objectReferenceValue = brightness;
            so.FindProperty("vibrationToggle").objectReferenceValue = vibration;
            so.FindProperty("fpsToggle").objectReferenceValue = fps;
            so.FindProperty("qualityLabel").objectReferenceValue = qualityLabel;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            Debug.Log($"[Opciones] prefab creado: {PrefabPath}");
        }

        private static RectTransform Row(RectTransform content, string label)
        {
            var row = NewRect("Fila_" + label, content, Vector2.zero, Vector2.one);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var text = NewText("Etiqueta", row, label, 40, TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            text.GetComponent<LayoutElement>().minWidth = 380f;
            return row;
        }

        private static Slider SliderRow(RectTransform content, string label)
        {
            var row = Row(content, label);
            var slider = CreateSlider(row);
            var element = slider.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 640f;
            element.preferredHeight = 60f;
            return slider;
        }

        private static Toggle ToggleRow(RectTransform content, string label)
        {
            var row = Row(content, label);

            var background = NewRect("Fondo", row, Vector2.zero, Vector2.one);
            var element = background.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 80f;
            element.preferredHeight = 80f;
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.9f, 0.85f, 0.75f, 1f);

            var check = NewRect("Marca", background, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.color = ButtonColor;

            var toggle = background.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private static TextMeshProUGUI QualityRow(RectTransform content, OptionsController controller)
        {
            var row = Row(content, "Calidad gráfica");

            var previous = MakeButton("Boton_CalidadAnterior", row, "<", 48, 100f, 90f);
            UnityEventTools.AddPersistentListener(previous.onClick, controller.PreviousQuality);

            var value = NewText("CalidadActual", row, "Media", 40, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            var valueElement = value.gameObject.AddComponent<LayoutElement>();
            valueElement.preferredWidth = 360f;
            valueElement.preferredHeight = 90f;

            var next = MakeButton("Boton_CalidadSiguiente", row, ">", 48, 100f, 90f);
            UnityEventTools.AddPersistentListener(next.onClick, controller.NextQuality);
            return value;
        }

        private static Slider CreateSlider(RectTransform parent)
        {
            var resources = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };

            var go = DefaultControls.CreateSlider(resources);
            go.name = "Slider";
            go.transform.SetParent(parent, false);

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transform.Find("Fill Area/Fill").GetComponent<Image>().color = ButtonColor;
            return slider;
        }

        // ---------- Menu ----------

        private static void BuildMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
            if (canvas == null || menu == null) { Debug.LogError("[Menu] falta Canvas o MenuController."); return; }

            // El prefab se carga DESPUES de abrir la escena (Unity descarta lo que nadie referencia al abrirla).
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError("[Menu] no existe el prefab PanelOpciones."); return; }

            var current = canvas.transform.Find("Panel_Opciones");
            if (current != null && current.GetComponent<OptionsController>() != null) { Debug.Log("[Menu] ya usa el panel nuevo."); return; }
            if (current != null) Object.DestroyImmediate(current.gameObject);

            var panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            panel.name = "Panel_Opciones";
            panel.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)panel.transform);

            var back = panel.transform.Find("Boton_Volver").GetComponent<Button>();
            UnityEventTools.AddPersistentListener(back.onClick, menu.HideOptions);

            var wipe = panel.GetComponentInChildren<WipeDataButton>(true);
            var wipeSo = new SerializedObject(wipe);
            wipeSo.FindProperty("menu").objectReferenceValue = menu;
            wipeSo.ApplyModifiedProperties();

            var menuSo = new SerializedObject(menu);
            menuSo.FindProperty("optionsPanel").objectReferenceValue = panel;
            menuSo.ApplyModifiedProperties();

            panel.SetActive(false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Menu] panel de opciones reemplazado por el nuevo.");
        }

        // ---------- pausa en los niveles ----------

        private static void BuildPause(string path, string exitScene)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) { Debug.LogError($"[{scene.name}] no hay Canvas."); return; }
            if (canvas.transform.Find("Boton_Pausa") != null) { Debug.Log($"[{scene.name}] ya tiene pausa."); return; }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) { Debug.LogError($"[{scene.name}] no existe el prefab PanelOpciones."); return; }

            var controllerGo = new GameObject("Pausa", typeof(PauseController));
            var pause = controllerGo.GetComponent<PauseController>();

            // Panel de pausa: Continuar / Opciones / Salir.
            var panel = NewRect("Panel_Pausa", canvas.transform, Vector2.zero, Vector2.one);
            panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.88f);
            NewText("Titulo", panel, "Pausa", 80, TextAlignmentOptions.Center, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f));
            AddPauseButton(panel, "Boton_Continuar", "Continuar", 0.58f, pause.Resume);
            AddPauseButton(panel, "Boton_Opciones", "Opciones", 0.44f, pause.ShowOptions);
            AddPauseButton(panel, "Boton_Salir", "Salir", 0.30f, pause.Exit);

            // Opciones: el mismo panel del Menu, sin el boton de borrar datos.
            var options = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            options.name = "Panel_OpcionesPausa";
            options.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)options.transform);
            UnityEventTools.AddPersistentListener(options.transform.Find("Boton_Volver").GetComponent<Button>().onClick, pause.HideOptions);
            options.GetComponentInChildren<WipeDataButton>(true).transform.parent.gameObject.SetActive(false);

            // Boton de pausa arriba a la izquierda.
            var button = MakeButton("Boton_Pausa", canvas.transform, "Pausa", 36, 190f, 90f);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f, -20f);
            UnityEventTools.AddPersistentListener(button.onClick, pause.Pause);

            var so = new SerializedObject(pause);
            so.FindProperty("pausePanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("optionsPanel").objectReferenceValue = options;
            so.FindProperty("exitScene").stringValue = exitScene;
            so.ApplyModifiedProperties();

            panel.gameObject.SetActive(false);
            options.SetActive(false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] pausa agregada (sale a {exitScene}).");
        }

        private static void AddPauseButton(RectTransform panel, string name, string text, float anchorY, UnityEngine.Events.UnityAction action)
        {
            var button = MakeButton(name, panel, text, 48, 640f, 120f);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        // ---------- helpers ----------

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Button MakeButton(string name, Transform parent, string text, float fontSize, float width, float height)
        {
            var rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(width, height);
            rect.gameObject.AddComponent<Image>().color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            rect.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            rect.GetComponent<LayoutElement>().preferredHeight = height;
            NewText("Label", rect, text, fontSize, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            return button;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
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
