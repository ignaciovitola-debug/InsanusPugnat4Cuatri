using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Arregla el Canvas de combate de la escena activa: pasa de "Constant
    /// Pixel Size" (que rompe el layout en pantallas angostas, como un build
    /// de Windows) a "Scale With Screen Size", usando como referencia la
    /// resolución con la que ya está visualmente ajustado el diseño
    /// (Note10 en horizontal, 2280x1080). Los botones no se tocan — solo el
    /// modo de escalado del Canvas.
    /// </summary>
    public static class CanvasResolutionFix
    {
        [MenuItem("Tools/Insanus Pugnat/Arreglar Canvas de Combate (Resolución)")]
        public static void FixCanvasScaling()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0)
            {
                Debug.LogError("[CanvasResolutionFix] No encontré ningún Canvas en la escena activa.");
                return;
            }

            int fixedCount = 0;
            foreach (var canvas in canvases)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) continue;

                Undo.RecordObject(scaler, "Arreglar escalado de Canvas");

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2280f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f; // prioriza igualar el ancho, que es como está pensado el layout

                fixedCount++;
                Debug.Log($"[CanvasResolutionFix] '{canvas.name}' actualizado a Scale With Screen Size (2280x1080, match ancho).");
            }

            if (fixedCount > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[CanvasResolutionFix] Listo. {fixedCount} Canvas actualizado(s).");
        }
    }
}
