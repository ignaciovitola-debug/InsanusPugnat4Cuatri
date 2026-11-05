using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: agrega el ShopInstaller al panel de la tienda y le pasa los items.
    public static class ShopInstallerTool
    {
        private const string ScenePath = "Assets/Scenes/Ludus.unity";
        private const string ShopFolder = "Assets/Data/Shop";

        [MenuItem("Tools/Temp/Install Shop MVP")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Los items se cargan despues de abrir la escena.
            var items = new[]
            {
                AssetDatabase.LoadAssetAtPath<ShopItem>($"{ShopFolder}/RecruitGladiator.asset"),
                AssetDatabase.LoadAssetAtPath<ShopItem>($"{ShopFolder}/RefillEnergy.asset"),
            };

            var view = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogError("[ShopMVP] No se encontro el ShopView en la Ludus");
                return;
            }

            var installer = view.GetComponent<ShopInstaller>();
            if (installer == null)
                installer = view.gameObject.AddComponent<ShopInstaller>();

            var so = new SerializedObject(installer);
            so.FindProperty("view").objectReferenceValue = view;
            var list = so.FindProperty("items");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Se relee lo asignado por si alguna referencia no quedo.
            var check = new SerializedObject(installer);
            bool ok = check.FindProperty("view").objectReferenceValue != null;
            for (int i = 0; i < items.Length; i++)
                ok &= check.FindProperty("items").GetArrayElementAtIndex(i).objectReferenceValue != null;

            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            EditorSceneManager.SaveScene(view.gameObject.scene);
            Debug.Log(ok ? "[ShopMVP] ShopInstaller agregado con view e items" : "[ShopMVP] ERROR: referencias vacias");
        }
    }
}
