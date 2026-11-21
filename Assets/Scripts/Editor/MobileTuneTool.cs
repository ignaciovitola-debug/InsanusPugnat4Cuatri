using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: aligera el juego para celulares de gama media (solo afecta a Android).
    public static class MobileTuneTool
    {
        private const string RefsPath = "Assets/Resources/RendererRefs.asset";

        // Texturas grandes que se usan: tope de tamaño solo en Android.
        private static readonly (string path, int max)[] Textures =
        {
            ("Assets/Art/Texturas/Arena_Normal.jpg", 1024),
            ("Assets/Art/Texturas/Arena_Color.jpg", 1024),
            ("Assets/Art/Texturas/Piso_Emblema_Plano.png", 1024),
            ("Assets/Art/Assets/letras/boton menu principal.png", 1024),
            ("Assets/Art/Assets/coins/Espadas romanas cruzadas ornamentadas.png", 512),
        };

        // Sonidos largos de ambiente: en Android se comprimen y se leen en streaming.
        private static readonly string[] LongSounds =
        {
            "Assets/Art/Sonidos/Gente1.mp3", "Assets/Art/Sonidos/Gente2.mp3",
            "Assets/Art/Sonidos/LLuvia.mp3", "Assets/Art/Sonidos/LLuvia2.mp3",
            "Assets/Art/Sonidos/Stadium1.mp3", "Assets/Art/Sonidos/Stadium2.mp3", "Assets/Art/Sonidos/Stadium3.mp3",
        };

        public static void Run()
        {
            CreateRendererRefs();
            LimitTextures();
            FixUncompressedTextures();
            ConfigureSounds();
            LightenMobilePipeline();
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileTune] listo");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void CreateRendererRefs()
        {
            var refs = AssetDatabase.LoadAssetAtPath<RendererRefs>(RefsPath);
            if (refs == null)
            {
                refs = ScriptableObject.CreateInstance<RendererRefs>();
                AssetDatabase.CreateAsset(refs, RefsPath);
            }

            refs.renderers = new[]
            {
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Mobile_Renderer.asset"),
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/PC_Renderer.asset"),
            };
            EditorUtility.SetDirty(refs);
            Debug.Log($"[MobileTune] RendererRefs con {refs.renderers.Length} renderers ({(refs.renderers[0] != null && refs.renderers[1] != null ? "ok" : "FALTA UNO")})");
        }

        private static void LimitTextures()
        {
            foreach (var (path, max) in Textures)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { Debug.LogWarning($"[MobileTune] no se encontro {path}"); continue; }

                var settings = importer.GetPlatformTextureSettings("Android");
                settings.overridden = true;
                settings.maxTextureSize = max;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                Debug.Log($"[MobileTune] {Path.GetFileName(path)}: Android max {max}");
            }
        }

        // Una textura de medidas raras con mipmaps no se comprime: sin mipmaps (no hacen falta con camara ortografica) si.
        private static void FixUncompressedTextures()
        {
            foreach (string path in new[] { "Assets/Art/Texturas/Piso_Emblema_Plano.png",
                                            "Assets/Art/Assets/coins/Espadas romanas cruzadas ornamentadas.png" })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                Debug.Log($"[MobileTune] {Path.GetFileName(path)}: sin mipmaps");
            }

            // La textura de arena pixelada pasa de compresion alta a normal (menos peso, mismo tamaño).
            var sand = AssetImporter.GetAtPath("Assets/Art/Modelos/Textura de arena pixelada con piedras.png") as TextureImporter;
            if (sand != null)
            {
                var settings = sand.GetPlatformTextureSettings("Android");
                settings.overridden = true;
                settings.maxTextureSize = 2048;
                settings.format = TextureImporterFormat.Automatic;
                settings.textureCompression = TextureImporterCompression.Compressed;
                sand.SetPlatformTextureSettings(settings);
                sand.SaveAndReimport();
                Debug.Log("[MobileTune] Textura de arena pixelada: compresion normal en Android");
            }
        }

        private static void ConfigureSounds()
        {
            foreach (string path in LongSounds)
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) { Debug.LogWarning($"[MobileTune] no se encontro {path}"); continue; }

                var sample = importer.GetOverrideSampleSettings("Android");
                sample.loadType = AudioClipLoadType.Streaming;
                sample.compressionFormat = AudioCompressionFormat.Vorbis;
                sample.quality = 0.5f;
                importer.SetOverrideSampleSettings("Android", sample);
                importer.SaveAndReimport();
                Debug.Log($"[MobileTune] {Path.GetFileName(path)}: Android streaming");
            }
        }

        // Sin HDR la pantalla usa menos memoria y ancho de banda; el resto del perfil queda igual.
        private static void LightenMobilePipeline()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (asset == null) { Debug.LogError("[MobileTune] no se encontro Mobile_RPAsset"); return; }

            var so = new SerializedObject(asset);
            so.FindProperty("m_SupportsHDR").boolValue = false;
            so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Debug.Log("[MobileTune] Mobile_RPAsset: HDR apagado, luces por objeto 2");
        }
    }
}
