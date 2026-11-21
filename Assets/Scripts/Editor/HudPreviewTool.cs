using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: dibuja las barras de vida sobre la arena y guarda una captura para revisar el diseño.
    public static class HudPreviewTool
    {
        private const string Output = @"C:\Users\MSI\AppData\Local\Temp\claude\3d7c7f0e5f0528c9\b3f6ebd3-b125-45b5-80ae-8c97ed4937b2\scratchpad\shots";

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/ArenaDeCombate2.unity", OpenSceneMode.Single);
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

            var flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            var hudType = typeof(HealthBarsHud);
            var hud = (HealthBarsHud)hudType.GetMethod("Create", flags).Invoke(null, null);

            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            // Rellena las dos barras con valores de ejemplo: tuya al 62% (con estela), rival al 28%.
            Fill(hudType, hud, "playerBar", "Tu Gladiador", 0.62f, 0.85f, 93f, 150f, flags);
            Fill(hudType, hud, "enemyBar", "Mirmillón", 0.28f, 0.5f, 36f, 130f, flags);

            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Output, "hud_barras.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = previous;
            Debug.Log("[HudPreview] captura guardada");
            EditorApplication.Exit(0);
        }

        // Captura de la Ludus con el panel de entrenamiento abierto y sus filas armadas.
        public static void RunTraining()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/Ludus.unity", OpenSceneMode.Single);
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            var panel = Object.FindFirstObjectByType<TrainingPanel>(FindObjectsInactive.Include);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var template = (TrainingRow)typeof(TrainingPanel).GetField("rowTemplate", flags).GetValue(panel);
            var content = (RectTransform)typeof(TrainingPanel).GetField("content", flags).GetValue(panel);
            var title = (TMPro.TMP_Text)typeof(TrainingPanel).GetField("statusLabel", flags).GetValue(panel);

            for (var p = panel.transform; p != null; p = p.parent)
                p.gameObject.SetActive(true);
            title.text = "Entrenar a Tu Gladiador";

            template.gameObject.SetActive(false);
            var tiers = EconomyConfig.Instance.trainingTiers;
            for (int i = 0; i < tiers.Length; i++)
            {
                var row = Object.Instantiate(template, content);
                row.gameObject.SetActive(true);
                row.Setup(i, tiers[i], _ => { });
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Output, "panel_entrenamiento.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Debug.Log("[HudPreview] captura panel de entrenamiento");
            EditorApplication.Exit(0);
        }

        // Captura de las escenas con su interfaz (los Canvas de pantalla se pasan a camara para poder dibujarlos).
        public static void RunScenes()
        {
            Directory.CreateDirectory(Output);
            foreach (string scene in new[] { "Ludus", "Menu" })
            {
                EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity", OpenSceneMode.Single);
                var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();

                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                }
                Canvas.ForceUpdateCanvases();

                var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(Output, $"escena_{scene}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                camera.targetTexture = null;
                Debug.Log($"[HudPreview] captura {scene}");
            }
            EditorApplication.Exit(0);
        }

        private static void Fill(System.Type hudType, HealthBarsHud hud, string field, string label, float ratio, float trail, float current, float max, BindingFlags flags)
        {
            var bar = hudType.GetField(field, flags).GetValue(hud);
            var barType = bar.GetType();
            barType.GetMethod("SetName", flags).Invoke(bar, new object[] { label });
            barType.GetField("shown", flags).SetValue(bar, ratio);
            barType.GetField("trailShown", flags).SetValue(bar, trail);
            barType.GetMethod("Apply", flags).Invoke(bar, new object[] { ratio, trail, current, max });
        }
    }
}
