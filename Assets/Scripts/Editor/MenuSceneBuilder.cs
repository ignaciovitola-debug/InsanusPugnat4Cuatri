using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Reconstruye la escena "Menu" (que hoy tiene UI de prueba de combate)
    /// como el menú principal real: Jugar / Opciones / Créditos / Salir.
    /// Mantiene Main Camera, Directional Light y EventSystem tal como están;
    /// borra el resto de los objetos de nivel raíz y arma contenido nuevo.
    /// Correr con la escena Menu abierta y activa.
    /// </summary>
    public static class MenuSceneBuilder
    {
        private const string LevelSceneName = "ArenaDeCombate1";
        private static readonly string[] Keep = { "Main Camera", "Directional Light", "EventSystem" };

        [MenuItem("Tools/Insanus Pugnat/Reconstruir Menu Principal")]
        public static void RebuildMenu()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Menu")
            {
                Debug.LogError("[MenuSceneBuilder] Abrí la escena 'Menu' y volvé a intentar (escena activa: " + scene.name + ").");
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                bool shouldKeep = false;
                foreach (var keepName in Keep)
                    if (root.name == keepName) { shouldKeep = true; break; }

                if (!shouldKeep)
                    Undo.DestroyObjectImmediate(root);
            }

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            CreateText(canvasGO.transform, "Titulo", "INSANUS PUGNAT", 80, new Vector2(0f, 600f), new Vector2(900f, 150f));

            Button playButton = CreateButton(canvasGO.transform, "Boton_Jugar", "Jugar", new Vector2(0f, 150f));
            Button optionsButton = CreateButton(canvasGO.transform, "Boton_Opciones", "Opciones", new Vector2(0f, 0f));
            Button creditsButton = CreateButton(canvasGO.transform, "Boton_Creditos", "Créditos", new Vector2(0f, -150f));
            Button quitButton = CreateButton(canvasGO.transform, "Boton_Salir", "Salir", new Vector2(0f, -300f));

            GameObject optionsPanel = CreatePanel(canvasGO.transform, "Panel_Opciones", "OPCIONES");
            Button optionsBackButton = CreateButton(optionsPanel.transform, "Boton_Volver", "Volver", new Vector2(0f, -500f));

            GameObject creditsPanel = CreatePanel(canvasGO.transform, "Panel_Creditos", "CRÉDITOS");
            CreateText(creditsPanel.transform, "Integrantes", "Vitola Ignacio - Matias Quijano - Emiliano Dacurso", 36, new Vector2(0f, 100f), new Vector2(900f, 100f));
            Button creditsBackButton = CreateButton(creditsPanel.transform, "Boton_Volver", "Volver", new Vector2(0f, -500f));

            var controllerGO = new GameObject("MenuController", typeof(MenuController));
            var controller = controllerGO.GetComponent<MenuController>();
            controller.Configure(optionsPanel, creditsPanel, LevelSceneName);

            UnityEventTools.AddPersistentListener(playButton.onClick, controller.PlayLevel);
            UnityEventTools.AddPersistentListener(optionsButton.onClick, controller.ShowOptions);
            UnityEventTools.AddPersistentListener(creditsButton.onClick, controller.ShowCredits);
            UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);
            UnityEventTools.AddPersistentListener(optionsBackButton.onClick, controller.HideOptions);
            UnityEventTools.AddPersistentListener(creditsBackButton.onClick, controller.HideCredits);

            Undo.RegisterCreatedObjectUndo(canvasGO, "Reconstruir Menu Principal");
            Undo.RegisterCreatedObjectUndo(controllerGO, "Reconstruir Menu Principal");

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[MenuSceneBuilder] Menú reconstruido: Jugar -> " + LevelSceneName + ", Opciones, Créditos, Salir. Guardá la escena cuando te guste el resultado.");
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2? size = null)
        {
            Vector2 actualSize = size ?? new Vector2(400f, 100f);

            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.35f, 0.12f, 0.08f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = actualSize;

            CreateText(go.transform, "Label", label, 40, Vector2.zero, actualSize);

            return go.GetComponent<Button>();
        }

        private static GameObject CreatePanel(Transform parent, string name, string titleText)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateText(go.transform, "Titulo", titleText, 64, new Vector2(0f, 600f), new Vector2(800f, 120f));

            go.SetActive(false);
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            return tmp;
        }

        [MenuItem("Tools/Insanus Pugnat/Agregar Controles a Opciones (Volumen + Calidad)")]
        public static void AddOptionsControls()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Menu")
            {
                Debug.LogError("[MenuSceneBuilder] Abrí la escena 'Menu' y volvé a intentar (escena activa: " + scene.name + ").");
                return;
            }

            GameObject panel = FindInScene("Panel_Opciones");
            if (panel == null)
            {
                Debug.LogError("[MenuSceneBuilder] No encontré 'Panel_Opciones'. Corré primero 'Reconstruir Menu Principal'.");
                return;
            }

            CreateText(panel.transform, "Label_Volumen", "Volumen", 36, new Vector2(-250f, 150f), new Vector2(300f, 60f));
            Slider volumeSlider = CreateSlider(panel.transform, "Slider_Volumen", new Vector2(150f, 150f), new Vector2(500f, 40f));

            CreateText(panel.transform, "Label_Calidad", "Calidad Gráfica", 36, new Vector2(-250f, 0f), new Vector2(300f, 60f));
            Button prevButton = CreateButton(panel.transform, "Boton_CalidadAnterior", "◀", new Vector2(50f, 0f), new Vector2(90f, 70f));
            Button nextButton = CreateButton(panel.transform, "Boton_CalidadSiguiente", "▶", new Vector2(400f, 0f), new Vector2(90f, 70f));
            TextMeshProUGUI qualityLabel = CreateText(panel.transform, "Label_CalidadActual", "Media", 36, new Vector2(225f, 0f), new Vector2(280f, 60f));

            var controllerGO = new GameObject("OptionsController", typeof(OptionsController));
            controllerGO.transform.SetParent(panel.transform, false);
            var controller = controllerGO.GetComponent<OptionsController>();
            controller.Configure(volumeSlider, qualityLabel);

            UnityEventTools.AddPersistentListener(nextButton.onClick, controller.NextQuality);
            UnityEventTools.AddPersistentListener(prevButton.onClick, controller.PreviousQuality);

            Undo.RegisterCreatedObjectUndo(controllerGO, "Agregar Controles a Opciones");
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[MenuSceneBuilder] Controles de Opciones agregados: volumen (slider) + calidad gráfica (flechas).");
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var sliderGO = new GameObject(name, typeof(Slider));
            sliderGO.transform.SetParent(parent, false);

            var rect = sliderGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var bgGO = new GameObject("Background", typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            bgGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            var fillGO = new GameObject("Fill", typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            fillGO.GetComponent<Image>().color = new Color(0.7f, 0.15f, 0.1f);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handleGO = new GameObject("Handle", typeof(Image));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            handleGO.GetComponent<Image>().color = Color.white;
            var handleRect = handleGO.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30f, 0f);

            slider.targetGraphic = handleGO.GetComponent<Image>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            return slider;
        }

        private static GameObject FindInScene(string name)
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var children = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in children)
                    if (t.name == name) return t.gameObject;
            }
            return null;
        }
    }
}
