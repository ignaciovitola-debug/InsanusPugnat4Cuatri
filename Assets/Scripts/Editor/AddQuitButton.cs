using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GladiusAI.EditorTools
{
    /// <summary>
    /// Agrega un botón "Salir" arriba a la derecha del Canvas de la escena
    /// activa, para que se pueda cerrar el juego después de probarlo
    /// (útil para que el profesor cierre el build al terminar de evaluar).
    /// </summary>
    public static class AddQuitButton
    {
        [MenuItem("Tools/Insanus Pugnat/Agregar Botón Salir")]
        public static void AddButton()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[AddQuitButton] No encontré ningún Canvas en la escena activa.");
                return;
            }

            var buttonGO = new GameObject("Boton_Salir", typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(canvas.transform, false);
            buttonGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(140f, 60f);

            var textGO = new GameObject("Label", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(buttonGO.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Salir";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var quitGO = new GameObject("QuitButton", typeof(QuitButton));
            quitGO.transform.SetParent(canvas.transform, false);
            var quit = quitGO.GetComponent<QuitButton>();

            var button = buttonGO.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, quit.Quit);

            Undo.RegisterCreatedObjectUndo(buttonGO, "Agregar Botón Salir");
            Undo.RegisterCreatedObjectUndo(quitGO, "Agregar Botón Salir");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[AddQuitButton] Botón 'Salir' agregado arriba a la derecha. Ajustalo a gusto.");
        }
    }
}
