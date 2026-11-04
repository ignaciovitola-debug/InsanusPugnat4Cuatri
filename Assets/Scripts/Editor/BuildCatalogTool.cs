using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: mejora el humo, arma el brasero (pilar + llama + humo), lo coloca
    // en las arenas, importa los modelos de Kenney y el UI Pack, y arma la escena Catalogo para revisar todo.
    // Borrar este archivo despues de usarla.
    public static class BuildCatalogTool
    {
        private const string PrefabFolder = "Assets/Prefabs/Efectos";
        private const string SmokePath = PrefabFolder + "/Humo.prefab";
        private const string BrazierPath = PrefabFolder + "/Brasero.prefab";
        private const string PillarPath = "Assets/Prefabs/Props/isometric-miniature-dungeon/Angle/stoneColumn_S.prefab";
        private const string CatalogScene = "Assets/Scenes/Catalogo.unity";
        private const string ModelsFolder = "Assets/Art/Modelos/KenneyMiniArena";
        private const string UiFolder = "Assets/Art/UI";

        private static readonly string[] Scenes =
        {
            "Assets/Scenes/ArenaDeCombate1.unity",
            "Assets/Scenes/ArenaDeCombate2.unity",
            "Assets/Scenes/Ludus.unity",
        };

        // Posiciones de los dos braseros (mismas en las tres escenas, delante del muro).
        private static readonly Vector3[] BrazierPositions =
        {
            new Vector3(-8.5f, -0.6f, 7f),
            new Vector3(7.5f, -0.6f, 7f),
        };

        [MenuItem("Tools/Insanus Pugnat/Paso 1: humo, braseros y catalogo")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string ideas = System.Environment.GetEnvironmentVariable("INSANUS_IDEAS");
            ImportExternalAssets(ideas);
            ImproveSmoke();
            CreateBrazier();

            string original = SceneManager.GetActiveScene().path;
            try
            {
                foreach (string path in Scenes)
                    PlaceBraziers(path);
                BuildCatalog();
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- importar lo descargado ----------

        private static void ImportExternalAssets(string ideas)
        {
            if (string.IsNullOrEmpty(ideas) || !Directory.Exists(ideas))
            {
                Debug.LogWarning("[Paso1] sin carpeta Ideas (INSANUS_IDEAS): se usan los assets que ya esten importados.");
                return;
            }

            CopyDirectory(Path.Combine(ideas, "kenney_mini-arena", "Models", "FBX format"), ModelsFolder, "*");
            CopyDirectory(Path.Combine(ideas, "kenney_ui-pack-rpg-expansion", "PNG"), UiFolder + "/KenneyRPG", "*.png");
            CopyFile(Path.Combine(ideas, "Logo-Roma.png"), UiFolder + "/Roma/Logo-Roma.png");
            CopyFile(Path.Combine(ideas, "Roma2.png"), UiFolder + "/Roma/Roma2.png");
            AssetDatabase.Refresh();

            // Todo el UI se importa como sprite; la imagen grande se baja de tamano para el celular.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                bool changed = importer.textureType != TextureImporterType.Sprite;
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                if (Path.GetFileName(path) == "Roma2.png" && importer.maxTextureSize != 1024)
                {
                    importer.maxTextureSize = 1024;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void CopyDirectory(string source, string target, string pattern)
        {
            if (!Directory.Exists(source)) { Debug.LogWarning($"[Paso1] no existe {source}"); return; }
            foreach (string file in Directory.GetFiles(source, pattern, SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta")) continue;
                CopyFile(file, Path.Combine(target, file.Substring(source.Length).TrimStart('\\', '/')).Replace('\\', '/'));
            }
        }

        private static void CopyFile(string source, string target)
        {
            if (!File.Exists(source) || File.Exists(target)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target);
        }

        // ---------- humo y brasero ----------

        private static void ImproveSmoke()
        {
            var root = PrefabUtility.LoadPrefabContents(SmokePath);
            var ps = root.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.47f, 0.44f, 0.65f), new Color(0.75f, 0.72f, 0.7f, 0.5f));
            main.maxParticles = 50;

            root.GetComponent<ParticleSystemRenderer>().sortingOrder = 22;

            PrefabUtility.SaveAsPrefabAsset(root, SmokePath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void CreateBrazier()
        {
            if (File.Exists(BrazierPath)) return;

            var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PillarPath);
            var smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePath);
            if (pillarPrefab == null || smokePrefab == null) { Debug.LogError("[Paso1] faltan el pilar o el humo."); return; }

            var root = new GameObject("Brasero");

            var pillar = (GameObject)PrefabUtility.InstantiatePrefab(pillarPrefab);
            pillar.name = "Pilar";
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localScale = Vector3.one * 0.4f;

            var flame = BuildFlame();
            flame.transform.SetParent(root.transform, false);
            flame.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            var smoke = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab);
            smoke.name = "Humo";
            smoke.transform.SetParent(root.transform, false);
            smoke.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, BrazierPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject BuildFlame()
        {
            var go = new GameObject("Llama", typeof(ParticleSystem));
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Particulas/M-Humo.mat");
            renderer.sortingOrder = 21;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration = 3f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.maxParticles = 30;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f, 0.9f), new Color(1f, 0.8f, 0.2f, 0.9f));

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.5f), new GradientColorKey(new Color(0.5f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            return go;
        }

        private static void PlaceBraziers(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (GameObject.Find("Braseros") != null) { Debug.Log($"[{scene.name}] ya tiene braseros."); return; }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BrazierPath);
            if (prefab == null) { Debug.LogError($"[{scene.name}] no existe el prefab Brasero."); return; }

            var root = new GameObject("Braseros");
            for (int i = 0; i < BrazierPositions.Length; i++)
            {
                var brazier = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                brazier.name = $"Brasero_{i + 1}";
                brazier.transform.SetParent(root.transform, false);
                brazier.transform.position = BrazierPositions[i];
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] {BrazierPositions.Length} braseros colocados.");
        }

        // ---------- catalogo ----------

        private static void BuildCatalog()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ExplorationCamera));
            cam.tag = "MainCamera";
            cam.transform.SetPositionAndRotation(new Vector3(18f, 10f, -8f), Quaternion.Euler(35f, 0f, 0f));
            var camera = cam.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            var camSo = new SerializedObject(cam.GetComponent<ExplorationCamera>());
            camSo.FindProperty("limitX").vector2Value = new Vector2(-4f, 44f);
            camSo.FindProperty("limitZ").vector2Value = new Vector2(-30f, 30f);
            camSo.FindProperty("maxSize").floatValue = 24f;
            camSo.ApplyModifiedProperties();

            var light = new GameObject("Luz", typeof(Light));
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.GetComponent<Light>().type = LightType.Directional;

            float z = 0f;
            foreach (string pack in new[] { "isometric-miniature-dungeon", "isometric-miniature-bases" })
            {
                string folder = $"Assets/Prefabs/Props/{pack}/Angle";
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith("_S.prefab"))
                    .OrderBy(p => p)
                    .ToList();

                AddLabel($"=== {pack} ===", new Vector3(-3f, 0f, z + 1.5f), 3f);
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]), scene);
                    var position = new Vector3((i % 12) * 3.6f, 0f, z - (i / 12) * 5.5f);
                    instance.transform.position = position;
                    AddLabel(Path.GetFileNameWithoutExtension(prefabs[i]).Replace("_S", ""), position + new Vector3(0f, 0f, -0.9f), 1.4f);
                }
                z -= (prefabs.Count / 12 + 2) * 5.5f;
            }

            AddLabel("=== Kenney Mini Arena (3D) ===", new Vector3(-3f, 0f, z + 1.5f), 3f);
            var models = AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
            for (int i = 0; i < models.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(models[i]), scene);
                var position = new Vector3((i % 12) * 3.6f, 0f, z - (i / 12) * 5f);
                instance.transform.position = position;
                instance.transform.localScale = Vector3.one * 2f;
                AddLabel(Path.GetFileNameWithoutExtension(models[i]), position + new Vector3(0f, 0f, -1.4f), 1.4f);
            }

            EditorSceneManager.SaveScene(scene, CatalogScene);
            Debug.Log($"[Catalogo] escena armada con props y {models.Count} modelos 3D.");
        }

        private static void AddLabel(string text, Vector3 position, float size)
        {
            var go = new GameObject("Etiqueta");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(3.4f, 1f);
        }
    }
}
