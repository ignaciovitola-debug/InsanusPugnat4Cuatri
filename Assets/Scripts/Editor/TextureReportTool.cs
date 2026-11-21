using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: muestra como queda importada cada textura grande para Android.
    public static class TextureReportTool
    {
        private static readonly string[] Paths =
        {
            "Assets/Art/Texturas/Piso_Emblema_Plano.png",
            "Assets/Art/Texturas/Arena_Normal.jpg",
            "Assets/Art/Texturas/Arena_Color.jpg",
            "Assets/Art/Modelos/Textura de arena pixelada con piedras.png",
            "Assets/Art/Assets/coins/Espadas romanas cruzadas ornamentadas.png",
            "Assets/Art/Assets/letras/boton menu principal.png",
            "Assets/Art/Modelos/fortress/textures/SimplePolygon_MultiColorPalette.png",
            "Assets/Resources/Effects/Sangre.png",
        };

        public static void Run()
        {
            Debug.Log($"[TexReport] plataforma activa: {EditorUserBuildSettings.activeBuildTarget}");
            foreach (string path in Paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || tex == null) { Debug.Log($"[TexReport] {path}: no se pudo leer"); continue; }

                importer.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
                var android = importer.GetPlatformTextureSettings("Android");
                var def = importer.GetDefaultPlatformTextureSettings();
                long bytes = Profiler.GetRuntimeMemorySizeLong(tex);
                Debug.Log($"[TexReport] {System.IO.Path.GetFileName(path)}: original {srcW}x{srcH} -> importada {tex.width}x{tex.height} formato {tex.format} mips {tex.mipmapCount} = {bytes / 1024} KB | " +
                          $"Android(over={android.overridden}, max={android.maxTextureSize}, comp={android.textureCompression}) default(max={def.maxTextureSize}, comp={def.textureCompression})");
            }
            EditorApplication.Exit(0);
        }
    }
}
