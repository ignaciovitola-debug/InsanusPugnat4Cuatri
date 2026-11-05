using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: agrega a la Ludus un boton "Menu" copiando el boton "?" y lo pone a su izquierda.
    public static class LudusMenuButtonTool
    {
        [MenuItem("Tools/Temp/Add Ludus Menu Button")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ludus.unity", OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<LudusController>(FindObjectsInactive.Include);
            var help = GameObject.Find("Boton_Ayuda");
            if (controller == null || help == null)
            {
                Debug.LogError("[MenuBtn] No se encontro LudusController o Boton_Ayuda");
                return;
            }

            if (GameObject.Find("Boton_Menu") != null)
            {
                Debug.Log("[MenuBtn] ya existe");
                return;
            }

            var copy = Object.Instantiate(help, help.transform.parent);
            copy.name = "Boton_Menu";

            var helpRect = (RectTransform)help.transform;
            var rect = (RectTransform)copy.transform;
            float width = 200f;
            float gap = 12f;
            float helpLeftEdge = helpRect.anchoredPosition.x - helpRect.sizeDelta.x * helpRect.pivot.x;
            rect.sizeDelta = new Vector2(width, helpRect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(helpLeftEdge - gap - width * (1f - rect.pivot.x), helpRect.anchoredPosition.y);

            var label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Menú";

            var button = copy.GetComponent<Button>();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddVoidPersistentListener(button.onClick, controller.BackToMenu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MenuBtn] Boton_Menu creado, listeners={button.onClick.GetPersistentEventCount()}, label={(label != null)}");
        }
    }
}
