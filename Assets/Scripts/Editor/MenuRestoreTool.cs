using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: saca el boton "Tienda" del menu y devuelve los demas botones a su lugar.
    public static class MenuRestoreTool
    {
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);

            var shop = GameObject.Find("Boton_Tienda");
            if (shop != null) Object.DestroyImmediate(shop);

            Place("Boton_Jugar", 150f);
            Place("Boton_Opciones", 0f);
            Place("Boton_Creditos", -150f);
            Place("Boton_Salir", -300f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MenuRestore] listo, Boton_Tienda {(GameObject.Find("Boton_Tienda") == null ? "borrado" : "SIGUE")}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Place(string name, float y)
        {
            var go = GameObject.Find(name);
            if (go == null) { Debug.LogError($"[MenuRestore] falta {name}"); return; }
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }
}
