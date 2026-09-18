using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Vuelca por Consola la configuración del/de los Canvas de la escena
    /// activa y de sus botones, para diagnosticar por qué algo se ve bien
    /// en el Editor (Simulator) pero se rompe en un build real de Windows
    /// (generalmente un tema de CanvasScaler / anchors mal configurados).
    /// </summary>
    public static class CanvasDiagnostics
    {
        [MenuItem("Tools/Insanus Pugnat/Diagnosticar Canvas de Combate")]
        public static void Diagnose()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0)
            {
                Debug.LogError("[CanvasDiagnostics] No encontré ningún Canvas en la escena activa.");
                return;
            }

            foreach (var canvas in canvases)
            {
                Debug.Log($"===== Canvas: {canvas.name} =====");
                Debug.Log($"Render Mode: {canvas.renderMode}");

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    Debug.Log($"UI Scale Mode: {scaler.uiScaleMode}");
                    Debug.Log($"Reference Resolution: {scaler.referenceResolution}");
                    Debug.Log($"Screen Match Mode: {scaler.screenMatchMode}");
                    Debug.Log($"Match (Width<->Height): {scaler.matchWidthOrHeight}");
                }
                else
                {
                    Debug.Log("(Este Canvas no tiene CanvasScaler)");
                }

                var buttons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    var rect = button.GetComponent<RectTransform>();
                    Debug.Log($"--- Botón: {button.name} ---");
                    Debug.Log($"  AnchorMin: {rect.anchorMin} | AnchorMax: {rect.anchorMax}");
                    Debug.Log($"  AnchoredPosition: {rect.anchoredPosition} | SizeDelta: {rect.sizeDelta}");
                    Debug.Log($"  OffsetMin: {rect.offsetMin} | OffsetMax: {rect.offsetMax}");

                    var layoutGroup = rect.parent != null ? rect.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() : null;
                    if (layoutGroup != null)
                        Debug.Log($"  (El padre '{rect.parent.name}' tiene un LayoutGroup: {layoutGroup.GetType().Name})");
                }
            }
        }
    }
}
