using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: crea AdsConfig y el item de la tienda que ofrece un anuncio, y lo conecta en la Ludus.
    // Los datos de LevelPlay se pasan por linea de comandos para no escribirlos en el codigo:
    //   -adsAppKey <clave> [-adsEnergyUnit <id>] [-adsCoinsUnit <id>]
    public static class AdsSetupTool
    {
        private const string ConfigPath = "Assets/Resources/AdsConfig.asset";
        private const string ItemPath = "Assets/Data/Shop/WatchEnergyAd.asset";

        public static void Run()
        {
            SetupConfig();
            SetupShopItem();
            AssetDatabase.SaveAssets();
            Debug.Log("[AdsSetup] listo");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static string Arg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static void SetupConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AdsConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AdsConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            string appKey = Arg("-adsAppKey");
            string energy = Arg("-adsEnergyUnit");
            string coins = Arg("-adsCoinsUnit");
            if (!string.IsNullOrEmpty(appKey)) config.appKey = appKey;
            if (!string.IsNullOrEmpty(energy)) config.energyAdUnitId = energy;
            if (!string.IsNullOrEmpty(coins)) config.coinsAdUnitId = coins;

            EditorUtility.SetDirty(config);
            Debug.Log($"[AdsSetup] AdsConfig: appKey {(config.IsConfigured ? "cargada" : "VACIA")}, energia {config.energyAdUnitId}, monedas {config.coinsAdUnitId}");
        }

        private static void SetupShopItem()
        {
            var item = AssetDatabase.LoadAssetAtPath<WatchAdItem>(ItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<WatchAdItem>();
                AssetDatabase.CreateAsset(item, ItemPath);
            }

            var so = new SerializedObject(item);
            so.FindProperty("displayName").stringValue = "Ver un anuncio";
            so.FindProperty("description").stringValue = "Mirá un anuncio entero y llená la energía. Si lo cerrás antes, ganás solo 1 punto.";
            so.FindProperty("price").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);

            // Los assets se cargan despues de abrir la escena.
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ludus.unity", OpenSceneMode.Single);
            item = AssetDatabase.LoadAssetAtPath<WatchAdItem>(ItemPath);
            var installer = Object.FindFirstObjectByType<ShopInstaller>(FindObjectsInactive.Include);
            if (installer == null) { Debug.LogError("[AdsSetup] no se encontro el ShopInstaller"); return; }

            var installerSo = new SerializedObject(installer);
            var list = installerSo.FindProperty("items");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
                {
                    Debug.Log("[AdsSetup] el item ya estaba en la tienda");
                    return;
                }
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            installerSo.ApplyModifiedPropertiesWithoutUndo();
            bool ok = list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue != null;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AdsSetup] item agregado a la tienda ({list.arraySize} items) {(ok ? "ok" : "ERROR: referencia vacia")}");
        }
    }
}
