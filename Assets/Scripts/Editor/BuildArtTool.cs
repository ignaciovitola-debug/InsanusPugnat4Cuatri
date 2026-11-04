using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: copia la textura de piso, genera las texturas de particulas,
    // arma los prefabs de efectos (confeti, petalos, humo) y los coloca en Arena 1, Arena 2 y Ludus.
    // Borrar este archivo despues de usarla.
    public static class BuildArtTool
    {
        private const string TexFolder = "Assets/Art/Texturas";
        private const string FxFolder = "Assets/Art/Particulas";
        private const string PrefabFolder = "Assets/Prefabs/Efectos";
        private const string FloorMaterialPath = TexFolder + "/M-SueloArena.mat";
        private const string SourceColor = "Ground029_1K-JPG_Color.jpg";
        private const string SourceNormal = "Ground029_1K-JPG_NormalGL.jpg";
        private const string ConfettiPath = PrefabFolder + "/Confeti.prefab";
        private const string PetalsPath = PrefabFolder + "/PetalosAmbientales.prefab";
        private const string SmokePath = PrefabFolder + "/Humo.prefab";
        private static readonly Vector3 FxPosition = new Vector3(0f, 7f, 0f);

        private static readonly string[] Scenes =
        {
            "Assets/Scenes/ArenaDeCombate1.unity",
            "Assets/Scenes/ArenaDeCombate2.unity",
            "Assets/Scenes/Ludus.unity",
        };

        [MenuItem("Tools/Insanus Pugnat/Arte: piso y particulas")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // La textura descargada vive fuera del proyecto: la ruta llega por variable de entorno.
            string sourceDir = System.Environment.GetEnvironmentVariable("INSANUS_TEX_SOURCE");
            CopyFloorTextures(sourceDir);
            CreateParticleTextures();
            CreateFloorMaterial();
            CreatePrefabs();
            AssetDatabase.SaveAssets();

            string original = SceneManager.GetActiveScene().path;
            try
            {
                foreach (string path in Scenes)
                    ApplyToScene(path, path.Contains("ArenaDeCombate"));
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- piso ----------

        private static void CopyFloorTextures(string sourceDir)
        {
            EnsureFolder(TexFolder);
            Copy(sourceDir, SourceColor, "Arena_Color.jpg");
            Copy(sourceDir, SourceNormal, "Arena_Normal.jpg");

            var normal = (TextureImporter)AssetImporter.GetAtPath($"{TexFolder}/Arena_Normal.jpg");
            if (normal != null && normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.maxTextureSize = 1024;
                normal.SaveAndReimport();
            }

            var color = (TextureImporter)AssetImporter.GetAtPath($"{TexFolder}/Arena_Color.jpg");
            if (color != null && color.maxTextureSize != 1024)
            {
                color.maxTextureSize = 1024;
                color.SaveAndReimport();
            }
        }

        private static void Copy(string sourceDir, string sourceName, string targetName)
        {
            string target = $"{TexFolder}/{targetName}";
            if (File.Exists(target)) return;
            if (string.IsNullOrEmpty(sourceDir) || !File.Exists(Path.Combine(sourceDir, sourceName)))
            {
                Debug.LogError($"[Arte] no encuentro {sourceName} (variable INSANUS_TEX_SOURCE).");
                return;
            }
            File.Copy(Path.Combine(sourceDir, sourceName), target);
            AssetDatabase.ImportAsset(target);
        }

        private static void CreateFloorMaterial()
        {
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/Arena_Color.jpg");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/Arena_Normal.jpg");
            if (color == null) { Debug.LogError("[Arte] falta la textura de color del piso."); return; }

            var material = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, FloorMaterialPath);
            }

            material.SetTexture("_BaseMap", color);
            material.SetTextureScale("_BaseMap", new Vector2(8f, 6f));
            material.SetColor("_BaseColor", Color.white);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", new Vector2(8f, 6f));
                material.SetFloat("_BumpScale", 0.8f);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", 0.05f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
        }

        // ---------- texturas de particulas ----------

        private static void CreateParticleTextures()
        {
            EnsureFolder(FxFolder);
            WriteTexture("Petalo.png", 64, 64, PetalPixel);
            WriteTexture("Humo.png", 128, 128, PuffPixel);
            WriteTexture("Confeti.png", 16, 16, ConfettiPixel);

            foreach (string name in new[] { "Petalo", "Humo", "Confeti" })
                GetOrCreateParticleMaterial(name);
        }

        private delegate Color PixelFunc(int x, int y, int w, int h);

        private static void WriteTexture(string fileName, int w, int h, PixelFunc pixel)
        {
            string path = $"{FxFolder}/{fileName}";
            if (File.Exists(path)) return;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, pixel(x, y, w, h));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        // Petalo: gota alargada, mas ancha hacia la punta, con un poco de sombra en los bordes.
        private static Color PetalPixel(int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w * 2f - 1f;
            float v = (y + 0.5f) / h * 2f - 1f;
            float t = (u + 0.9f) / 1.8f;
            if (t <= 0f || t >= 1f) return new Color(1f, 1f, 1f, 0f);

            float halfWidth = 0.45f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 1.4f));
            float alpha = Mathf.Clamp01((halfWidth - Mathf.Abs(v)) * 10f);
            float shade = Mathf.Lerp(0.8f, 1f, t) * (1f - 0.15f * Mathf.Abs(v) / Mathf.Max(halfWidth, 0.01f));
            return new Color(shade, shade, shade, alpha);
        }

        // Humo: mancha redonda y suave con un poco de ruido para que parezca nube.
        private static Color PuffPixel(int x, int y, int w, int h)
        {
            float dx = (x + 0.5f) / w * 2f - 1f;
            float dy = (y + 0.5f) / h * 2f - 1f;
            float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            float alpha = falloff * falloff * (3f - 2f * falloff);
            alpha *= Mathf.Lerp(0.6f, 1.2f, Mathf.PerlinNoise(x * 0.05f + 3f, y * 0.05f + 7f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        private static Color ConfettiPixel(int x, int y, int w, int h)
        {
            bool inside = x >= 2 && x <= 13 && y >= 4 && y <= 11;
            return new Color(1f, 1f, 1f, inside ? 1f : 0f);
        }

        private static Material GetOrCreateParticleMaterial(string name)
        {
            string path = $"{FxFolder}/M-{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FxFolder}/{name}.png");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------- prefabs de efectos ----------

        private static void CreatePrefabs()
        {
            EnsureFolder(PrefabFolder);
            if (!File.Exists(ConfettiPath)) Save(BuildConfetti(), ConfettiPath);
            if (!File.Exists(PetalsPath)) Save(BuildPetals(), PetalsPath);
            if (!File.Exists(SmokePath)) Save(BuildSmoke(), SmokePath);
        }

        private static void Save(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static ParticleSystem NewSystem(string name, string materialName, int sortingOrder)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetOrCreateParticleMaterial(materialName);
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            return ps;
        }

        private static ParticleSystem.MinMaxGradient RandomFrom(params Color[] colors)
        {
            var gradient = new Gradient { mode = GradientMode.Fixed };
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                keys[i] = new GradientColorKey(colors[i], (i + 1f) / colors.Length);
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }

        private static Gradient FadeInOut(float fadeIn, float fadeOutStart)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static GameObject BuildConfetti()
        {
            var ps = NewSystem("Confeti", "Confeti", 20);

            var main = ps.main;
            main.duration = 2.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.25f;
            main.maxParticles = 200;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startColor = RandomFrom(
                new Color(0.95f, 0.2f, 0.2f), new Color(0.95f, 0.8f, 0.2f), new Color(0.2f, 0.5f, 0.95f),
                new Color(0.2f, 0.8f, 0.4f), Color.white, new Color(0.9f, 0.3f, 0.8f));

            var emission = ps.emission;
            emission.rateOverTime = 25f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)80) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 1f, 14f);

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2f, Mathf.PI * 2f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.5f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = FadeInOut(0.02f, 0.8f);

            return ps.gameObject;
        }

        private static GameObject BuildPetals()
        {
            var ps = NewSystem("PetalosAmbientales", "Petalo", 25);

            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.04f;
            main.maxParticles = 70;
            main.startColor = RandomFrom(
                new Color(0.86f, 0.12f, 0.25f), new Color(0.95f, 0.45f, 0.6f), new Color(1f, 0.85f, 0.9f),
                new Color(1f, 1f, 1f), new Color(0.98f, 0.8f, 0.3f));

            var emission = ps.emission;
            emission.rateOverTime = 4f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 1f, 16f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.8f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.8f;
            limit.dampen = 0.3f;

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI / 2f, Mathf.PI / 2f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.2f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = FadeInOut(0.1f, 0.85f);

            return ps.gameObject;
        }

        private static GameObject BuildSmoke()
        {
            var ps = NewSystem("Humo", "Humo", 15);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = 40;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.35f, 0.35f, 0.5f), new Color(0.6f, 0.6f, 0.6f, 0.4f));

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f)));

            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-Mathf.PI / 6f, Mathf.PI / 6f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = FadeInOut(0.15f, 0.4f);

            return ps.gameObject;
        }

        // ---------- escenas ----------

        private static void ApplyToScene(string path, bool isArena)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;

            // Los assets se cargan DESPUES de abrir la escena (Unity descarta lo que nadie referencia al abrirla).
            var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            var floor = GameObject.Find("Suelo");
            var floorRenderer = floor != null ? floor.GetComponent<Renderer>() : null;
            if (floorRenderer != null && floorMaterial != null && floorRenderer.sharedMaterial != floorMaterial)
            {
                floorRenderer.sharedMaterial = floorMaterial;
                changed = true;
            }

            if (GameObject.Find("PetalosAmbientales") == null)
            {
                var petals = AssetDatabase.LoadAssetAtPath<GameObject>(PetalsPath);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(petals, scene);
                instance.transform.position = FxPosition;
                changed = true;
            }

            if (isArena && Object.FindFirstObjectByType<VictoryEffects>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("EfectosVictoria", typeof(VictoryEffects));
                go.transform.position = FxPosition;
                var so = new SerializedObject(go.GetComponent<VictoryEffects>());
                so.FindProperty("confettiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPath).GetComponent<ParticleSystem>();
                so.ApplyModifiedProperties();
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] {(changed ? "piso con textura, petalos" + (isArena ? " y confeti de victoria" : "") + " aplicados" : "ya estaba al dia")}.");
        }

        private static void EnsureFolder(string folder)
        {
            if (Directory.Exists(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
