using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: arregla las filas encimadas de los paneles de la Ludus, pone texturas de
    // roca a las zonas nuevas, decora las arenas con modelos 3D de Kenney y arma el arte del Menu (logo, Cesar,
    // coliseo, patron). Borrar este archivo despues de usarla.
    public static class BuildArtTool2
    {
        private const string TexFolder = "Assets/Art/Texturas";
        private const string RomaFolder = "Assets/Art/UI/Roma";
        private const string ModelFolder = "Assets/Art/Modelos/KenneyMiniArena";
        private const string DecorName = "DecoracionKenney";

        private struct Piece
        {
            public string model;
            public Vector3 position;
            public float yaw;
            public float scale;
            public Piece(string model, float x, float y, float z, float yaw, float scale)
            {
                this.model = model; position = new Vector3(x, y, z); this.yaw = yaw; this.scale = scale;
            }
        }

        // Todo sobre el borde del escenario (muro de atras y costados), fuera de la zona de pelea.
        // Ignacio las mueve a mano y avisa cuales dejar.
        private static readonly Piece[] Decoration =
        {
            new Piece("statue", -11.5f, -0.6f, 6.5f, 180f, 2f),
            new Piece("statue", 10.5f, -0.6f, 6.5f, 180f, 2f),
            new Piece("weapon-rack", -4.5f, -0.6f, 8.0f, 180f, 1.8f),
            new Piece("weapon-rack", 3.5f, -0.6f, 8.0f, 180f, 1.8f),
            new Piece("trophy", -0.5f, -0.6f, 8.2f, 180f, 1.6f),
            new Piece("banner", -7.5f, 1.0f, 8.6f, 180f, 2f),
            new Piece("banner", -2.5f, 1.0f, 8.6f, 180f, 2f),
            new Piece("banner", 2.5f, 1.0f, 8.6f, 180f, 2f),
            new Piece("banner", 7.5f, 1.0f, 8.6f, 180f, 2f),
            new Piece("column-damaged", -12.5f, -0.6f, 1.5f, 0f, 2.2f),
            new Piece("column", 11.5f, -0.6f, 1.5f, 0f, 2.2f),
        };

        private static readonly string[] DecoratedScenes =
        {
            "Assets/Scenes/ArenaDeCombate1.unity",
            "Assets/Scenes/ArenaDeCombate2.unity",
            "Assets/Scenes/ArenaZona_Catacumbas.unity",
            "Assets/Scenes/ArenaZona_Patio.unity",
        };

        [MenuItem("Tools/Insanus Pugnat/Paso 5: rocas, decoracion y arte del menu")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string ideas = System.Environment.GetEnvironmentVariable("INSANUS_IDEAS");
            ImportTextures(ideas);
            ImportMenuArt(ideas);

            string original = SceneManager.GetActiveScene().path;
            try
            {
                FixLudusLayouts();
                foreach (string path in DecoratedScenes)
                    DecorateScene(path);
                BuildMenuArt();
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- importar ----------

        private static void ImportTextures(string ideas)
        {
            if (string.IsNullOrEmpty(ideas)) return;
            Copy(Path.Combine(ideas, "Rock1", "Rocks019_1K-JPG_Color.jpg"), $"{TexFolder}/Roca1_Color.jpg");
            Copy(Path.Combine(ideas, "Rock1", "Rocks019_1K-JPG_NormalGL.jpg"), $"{TexFolder}/Roca1_Normal.jpg");
            Copy(Path.Combine(ideas, "Rock2", "Rocks021_1K-JPG_Color.jpg"), $"{TexFolder}/Roca2_Color.jpg");
            Copy(Path.Combine(ideas, "Rock2", "Rocks021_1K-JPG_NormalGL.jpg"), $"{TexFolder}/Roca2_Normal.jpg");
            AssetDatabase.Refresh();

            foreach (string name in new[] { "Roca1_Normal", "Roca2_Normal" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath($"{TexFolder}/{name}.jpg");
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                }
            }

            CreateFloorMaterial("M-SueloCatacumbas", "Roca1");
            CreateFloorMaterial("M-SueloPatio", "Roca2");
            AssetDatabase.SaveAssets();
        }

        private static void CreateFloorMaterial(string materialName, string textureBase)
        {
            string path = $"{TexFolder}/{materialName}.mat";
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/{textureBase}_Color.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/{textureBase}_Normal.jpg");
            if (color == null) { Debug.LogError($"[Arte2] falta {textureBase}_Color.jpg"); return; }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", color);
            material.SetTextureScale("_BaseMap", new Vector2(8f, 6f));
            material.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f));
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", new Vector2(8f, 6f));
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
        }

        private static void ImportMenuArt(string ideas)
        {
            if (string.IsNullOrEmpty(ideas)) return;
            Copy(Path.Combine(ideas, "Coliseo1.png"), $"{RomaFolder}/Coliseo1.png");
            Copy(Path.Combine(ideas, "Coliseo2.png"), $"{RomaFolder}/Coliseo2.png");
            Copy(Path.Combine(ideas, "Zw.png"), $"{RomaFolder}/Cesar.png");
            Copy(Path.Combine(ideas, "Diseño1.jpeg"), $"{RomaFolder}/Greca.jpeg");
            Copy(Path.Combine(ideas, "Diseño2.jpeg"), $"{RomaFolder}/Patron.jpeg");
            AssetDatabase.Refresh();

            foreach (string name in new[] { "Coliseo1.png", "Coliseo2.png", "Cesar.png", "Greca.jpeg", "Patron.jpeg", "Logo-Roma.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath($"{RomaFolder}/{name}");
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static void Copy(string source, string target)
        {
            if (!File.Exists(source)) { Debug.LogWarning($"[Arte2] no existe {source}"); return; }
            if (File.Exists(target)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target);
        }

        // ---------- Ludus: filas ----------

        private static void FixLudusLayouts()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Ludus.unity", OpenSceneMode.Single);
            bool changed = false;
            foreach (string path in new[] { "Panel_Combates/Content", "Panel_Entrenamiento/Content" })
            {
                var content = FindDeep(path);
                var layout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
                if (layout == null || layout.childControlHeight) continue;

                // Con la altura controlada por el grupo, cada fila toma el alto de su LayoutElement y ya no se encima.
                layout.childControlHeight = true;
                EditorUtility.SetDirty(layout);
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log(changed ? "[Ludus] filas de los paneles corregidas." : "[Ludus] las filas ya estaban bien.");
        }

        private static Transform FindDeep(string path)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var found = canvas.transform.Find(path);
                if (found != null) return found;
            }
            return null;
        }

        // ---------- decoracion y pisos ----------

        private static void DecorateScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;

            // Los assets se cargan DESPUES de abrir la escena (Unity descarta lo que nadie referencia al abrirla).
            string floorMaterial = path.Contains("Catacumbas") ? "M-SueloCatacumbas" : path.Contains("Patio") ? "M-SueloPatio" : null;
            if (floorMaterial != null)
            {
                var floor = GameObject.Find("Suelo");
                var renderer = floor != null ? floor.GetComponent<Renderer>() : null;
                var material = AssetDatabase.LoadAssetAtPath<Material>($"{TexFolder}/{floorMaterial}.mat");
                if (renderer != null && material != null && renderer.sharedMaterial != material)
                {
                    renderer.sharedMaterial = material;
                    changed = true;
                }
            }

            if (GameObject.Find(DecorName) == null)
            {
                var root = new GameObject(DecorName);
                foreach (var piece in Decoration)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{piece.model}.fbx");
                    if (model == null) { Debug.LogWarning($"[Arte2] falta el modelo {piece.model}"); continue; }

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                    instance.name = piece.model;
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.position = piece.position;
                    instance.transform.rotation = Quaternion.Euler(0f, piece.yaw, 0f);
                    instance.transform.localScale = Vector3.one * piece.scale;
                }
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] {(changed ? "decoracion 3D" + (floorMaterial != null ? " y piso de roca" : "") + " aplicados" : "ya estaba al dia")}.");
        }

        // ---------- Menu ----------

        private static void BuildMenuArt()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) { Debug.LogError("[Menu] no hay Canvas."); return; }
            if (canvas.transform.Find("Arte_Fondo") != null) { Debug.Log("[Menu] ya tiene el arte."); return; }

            // Los sprites se cargan DESPUES de abrir la escena.
            var pattern = LoadSprite("Patron.jpeg");
            var colosseum = LoadSprite("Coliseo1.png");
            var logo = LoadSprite("Logo-Roma.png");
            var caesar = LoadSprite("Cesar.png");

            int order = 0;
            var background = AddImage(canvas.transform, "Arte_Fondo", pattern, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.35f), order++);
            background.type = Image.Type.Tiled;
            background.pixelsPerUnitMultiplier = 1.2f;

            AddImage(canvas.transform, "Arte_Coliseo", colosseum, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), new Color(1f, 1f, 1f, 0.22f), order++);
            AddImage(canvas.transform, "Arte_Logo", logo, new Vector2(0.01f, 0.22f), new Vector2(0.27f, 0.78f), Color.white, order++);
            AddImage(canvas.transform, "Arte_Cesar", caesar, new Vector2(0.73f, 0.08f), new Vector2(0.99f, 0.92f), Color.white, order++);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Menu] arte romano agregado (patron, coliseo, logo SPQR, Cesar).");
        }

        private static Sprite LoadSprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{RomaFolder}/{file}");

        private static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Color color, int siblingIndex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            go.transform.SetSiblingIndex(siblingIndex);
            return image;
        }
    }
}
