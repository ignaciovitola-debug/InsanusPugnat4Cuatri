using System.IO;
using UnityEditor;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso: crea los arquetipos y las dificultades y los asigna a EconomyConfig.
    // Borrar este archivo despues de usarla.
    public static class BuildPatternAssetsTool
    {
        private const string ArchetypeFolder = "Assets/Data/Archetypes";
        private const string DifficultyFolder = "Assets/Data/Difficulty";
        private const string ConfigPath = "Assets/Resources/EconomyConfig.asset";

        [MenuItem("Tools/Insanus Pugnat/Patrones: crear arquetipos y dificultades")]
        public static void Run()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(ArchetypeFolder);
            EnsureFolder(DifficultyFolder);

            var archetypes = new[]
            {
                CreateArchetype("Recluta", "Recluta", 85f, 8f, 15f, 1.3f, 1),
                CreateArchetype("Mirmillon", "Mirmillón", 130f, 8f, 14f, 1.5f, 2),
                CreateArchetype("Retiario", "Retiario", 70f, 9f, 17f, 0.9f, 3),
                CreateArchetype("Tracio", "Tracio", 100f, 12f, 20f, 1.2f, 4),
            };

            var difficulties = new DifficultyStrategy[]
            {
                CreateDifficulty<EasyDifficulty>("Easy", "Fácil"),
                CreateDifficulty<NormalDifficulty>("Normal", "Normal"),
                CreateDifficulty<HardDifficulty>("Hard", "Difícil"),
            };
            AssetDatabase.SaveAssets();

            var config = AssetDatabase.LoadAssetAtPath<EconomyConfig>(ConfigPath);
            if (config == null) { Debug.LogError("[Patrones] no existe EconomyConfig."); return; }

            var so = new SerializedObject(config);
            Assign(so.FindProperty("archetypes"), archetypes);
            Assign(so.FindProperty("difficulties"), difficulties);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Patrones] {archetypes.Length} arquetipos y {difficulties.Length} dificultades asignados a EconomyConfig.");
        }

        private static void Assign(SerializedProperty list, Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static EnemyArchetype CreateArchetype(string file, string title, float hp, float minDamage, float maxDamage, float cooldown, int skin)
        {
            string path = $"{ArchetypeFolder}/{file}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<EnemyArchetype>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = title;
            so.FindProperty("maxHP").floatValue = hp;
            so.FindProperty("minDamage").floatValue = minDamage;
            so.FindProperty("maxDamage").floatValue = maxDamage;
            so.FindProperty("attackCooldown").floatValue = cooldown;
            so.FindProperty("skinIndex").intValue = skin;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T CreateDifficulty<T>(string file, string title) where T : DifficultyStrategy
        {
            string path = $"{DifficultyFolder}/{file}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (Directory.Exists(folder)) return;
            AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        }
    }
}
