using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta de autoria: prepara los PNG de los packs como props parados para la vista 2.5D
    // (pivot en la base de lo que se ve, escala comun) y crea un prefab por cada uno.
    public class Props25DWindow : EditorWindow
    {
        private const string PrefabRoot = "Assets/Prefabs/Props";
        private const byte AlphaThreshold = 10;

        private float pixelsPerUnit = 60f;
        private bool createPrefabs = true;

        [MenuItem("Tools/Insanus Pugnat/Props 2.5D")]
        private static void Open() => GetWindow<Props25DWindow>("Props 2.5D");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1) En la ventana Project, selecciona una o mas carpetas con PNG (por ejemplo isometric-miniature-dungeon/Angle).\n" +
                "2) Ajusta los Pixels Per Unit: mas alto = props mas chicos.\n" +
                "3) Apreta Procesar. Los prefabs quedan en Assets/Prefabs/Props.", MessageType.Info);

            pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", pixelsPerUnit);
            createPrefabs = EditorGUILayout.Toggle("Crear prefabs", createPrefabs);

            if (GUILayout.Button("Procesar carpetas seleccionadas"))
                Process();
        }

        private void Process()
        {
            var folders = new List<string>();
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                    folders.Add(path);
            }

            if (folders.Count == 0)
            {
                Debug.LogWarning("Selecciona al menos una carpeta en la ventana Project.");
                return;
            }

            // Un PNG que ya usa una escena, prefab o material no se toca: cambiarle la escala o el pivote
            // agranda o corre lo que ya esta armado (le paso a los muros del juego).
            var alreadyUsed = CollectUsedTextures();

            var pngs = new List<string>();
            int skipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png")) continue;

                if (alreadyUsed.Contains(path)) skipped++;
                else pngs.Add(path);
            }
            if (skipped > 0)
                Debug.Log($"Props 2.5D: {skipped} PNG ya estan en uso en el juego y no se tocaron.");

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < pngs.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Props 2.5D", pngs[i], (float)i / pngs.Count);
                    ConfigureImport(pngs[i]);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            int created = 0;
            if (createPrefabs)
            {
                foreach (string path in pngs)
                    if (CreatePrefab(path)) created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Props 2.5D: {pngs.Count} PNG configurados, {created} prefabs nuevos.");
        }

        private static HashSet<string> CollectUsedTextures()
        {
            var used = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab t:Material"))
            {
                string owner = AssetDatabase.GUIDToAssetPath(guid);
                if (owner.StartsWith(PrefabRoot)) continue;

                foreach (string dependency in AssetDatabase.GetDependencies(owner, false))
                    if (dependency.EndsWith(".png"))
                        used.Add(dependency);
            }
            return used;
        }

        private void ConfigureImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot = new Vector2(0.5f, FindBasePivotY(path));
            importer.SaveAndReimport();
        }

        // El pivot queda en la fila mas baja con pixeles visibles: asi el objeto "pisa" el piso donde se lo coloque.
        private static float FindBasePivotY(string path)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = tex.GetPixels32();
            int width = tex.width, height = tex.height;
            DestroyImmediate(tex);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (pixels[y * width + x].a > AlphaThreshold)
                        return (float)y / height;

            return 0f;
        }

        private static bool CreatePrefab(string pngPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null) return false;

            string pack = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(pngPath)));
            string variant = Path.GetFileName(Path.GetDirectoryName(pngPath));
            string folder = $"{PrefabRoot}/{pack}/{variant}";
            EnsureFolder(folder);

            string prefabPath = $"{folder}/{Path.GetFileNameWithoutExtension(pngPath)}.prefab";
            if (File.Exists(prefabPath)) return false;

            var go = new GameObject(Path.GetFileNameWithoutExtension(pngPath));
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
            return true;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
