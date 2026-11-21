using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso: deja la configuracion de Android lista para celular y genera el APK.
    // Borrar este archivo despues de usarla.
    public static class BuildApkTool
    {
        private const string OutputPath = "D:/GladiusAI/Build/InsanusPugnat.apk";

        // Usa las escenas tildadas en Build Settings, asi nunca falta una (por ejemplo las zonas de combate).
        private static string[] Scenes()
        {
            var paths = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) paths.Add(scene.path);
            return paths.ToArray();
        }

        [MenuItem("Tools/Insanus Pugnat/Generar APK")]
        public static void Run()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.companyName = "VitolaQuijano";
            PlayerSettings.productName = "Insanus Pugnat";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.vitolaquijano.insanuspugnat");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 3;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });

            Debug.Log($"APK: resultado {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errores -> {OutputPath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
