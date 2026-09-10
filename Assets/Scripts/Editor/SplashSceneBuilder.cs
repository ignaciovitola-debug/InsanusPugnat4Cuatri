using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Crea la escena de Splash pedida por la consigna de Aplicación de
    /// Motores 2 (logo/título + integrantes del equipo, pasa sola al Menú).
    /// No toca ninguna escena existente: la crea, la guarda en
    /// Assets/Scenes/Splash.unity y la agrega a Build Settings en el primer
    /// lugar. Correr una sola vez; si ya existe, sobreescribe el archivo.
    /// </summary>
    public static class SplashSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Splash.unity";

        [MenuItem("Tools/Insanus Pugnat/Crear Escena de Splash")]
        public static void CreateSplashScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            EditorSceneManager.MoveGameObjectToScene(canvasGO, scene);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Fondo simple (placeholder — cambialo por el logo/ilustración cuando quieras)
            var bgGO = new GameObject("Fondo", typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            bgGO.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            CreateText(canvasGO.transform, "Titulo", "INSANUS PUGNAT", 96, new Vector2(0f, 200f), new Vector2(900f, 150f));
            CreateText(canvasGO.transform, "Integrantes", "Vitola Ignacio - Matias Quijano - Emiliano Dacurso", 40, new Vector2(0f, -250f), new Vector2(900f, 100f));

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                EditorSceneManager.MoveGameObjectToScene(esGO, scene);
            }

            var controllerGO = new GameObject("SplashController", typeof(SplashController));
            EditorSceneManager.MoveGameObjectToScene(controllerGO, scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsFirst(ScenePath);
            EditorSceneManager.CloseScene(scene, true);

            Debug.Log($"[SplashSceneBuilder] Escena creada en {ScenePath} y agregada a Build Settings en primer lugar. " +
                "Abrila para ajustar textos, colores o meter el logo real.");
        }

        private static void CreateText(Transform parent, string name, string content, int fontSize, Vector2 anchoredPos, Vector2 size)
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
        }

        private static void AddSceneToBuildSettingsFirst(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == path) return; // ya está agregada

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(path, true);
            for (int i = 0; i < scenes.Length; i++)
                newScenes[i + 1] = scenes[i];

            EditorBuildSettings.scenes = newScenes;
        }
    }
}
