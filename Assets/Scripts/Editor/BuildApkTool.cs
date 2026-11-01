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

        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Splash.unity",
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/ArenaDeCombate1.unity",
            "Assets/Scenes/Ludus.unity",
            "Assets/Scenes/ArenaDeCombate2.unity",
        };

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
            PlayerSettings.Android.bundleVersionCode = 2;

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes,
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
