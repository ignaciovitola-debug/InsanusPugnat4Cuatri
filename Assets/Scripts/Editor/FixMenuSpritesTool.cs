using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: arregla los sprites del menu (modo Single) y los vuelve a asignar.
    public static class FixMenuSpritesTool
    {
        private const string Folder = "Assets/Art/UI/Roma";

        [MenuItem("Tools/Temp/Fix Menu Sprites")]
        public static void Run()
        {
            foreach (string file in new[] { "Coliseo1.png", "Coliseo2.png", "Cesar.png", "Greca.jpeg", "Patron.jpeg", "Logo-Roma.png" })
            {
                var importer = AssetImporter.GetAtPath($"{Folder}/{file}") as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);

            // Los sprites se cargan despues de abrir la escena.
            var map = new (string objectName, string file)[]
            {
                ("Arte_Fondo", "Patron.jpeg"),
                ("Arte_Coliseo", "Coliseo1.png"),
                ("Arte_Logo", "Logo-Roma.png"),
                ("Arte_Cesar", "Cesar.png"),
            };

            bool allOk = true;
            foreach (var (objectName, file) in map)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{file}");
                var go = GameObject.Find(objectName);
                var image = go != null ? go.GetComponent<Image>() : null;
                if (sprite == null || image == null)
                {
                    Debug.LogError($"[MenuFix] {objectName}: sprite={(sprite != null)} image={(image != null)}");
                    allOk = false;
                    continue;
                }

                image.sprite = sprite;
                EditorUtility.SetDirty(image);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(allOk ? "[MenuFix] sprites del menu asignados" : "[MenuFix] hubo errores");
        }
    }
}
