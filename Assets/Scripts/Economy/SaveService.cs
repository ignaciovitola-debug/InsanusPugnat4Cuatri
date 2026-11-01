using System;
using System.IO;
using UnityEngine;

namespace GladiusAI
{
    // Guarda y carga el progreso como JSON en la carpeta de datos del celular.
    // Si el archivo no existe o esta danado, se arranca una partida nueva.
    public static class SaveService
    {
        private static SaveData data;

        private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static SaveData Data
        {
            get
            {
                if (data == null)
                    data = LoadOrCreate();
                return data;
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] No se pudo guardar: {e.Message}");
            }
        }

        // Borra el archivo y descarta lo cargado: el proximo acceso arranca una partida nueva.
        public static void Wipe()
        {
            data = null;
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] No se pudo borrar el archivo: {e.Message}");
            }
        }

        private static SaveData LoadOrCreate()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                        return loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Archivo ilegible, se empieza de cero: {e.Message}");
            }

            return new SaveData
            {
                energy = EconomyConfig.Instance.maxEnergy,
                energyStampTicks = DateTime.UtcNow.Ticks,
            };
        }

        // Con "Enter Play Mode" sin recargar dominio, los static sobreviven entre sesiones de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => data = null;
    }
}
