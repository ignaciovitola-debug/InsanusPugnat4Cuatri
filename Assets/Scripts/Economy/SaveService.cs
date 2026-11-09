using System;
using System.IO;
using UnityEngine;

namespace GladiusAI
{
    // Caretaker del Memento: guarda y carga las fotos en un archivo JSON.
    // Nunca toca el estado de los originadores, solo les pide su foto y se la devuelve.
    public static class SaveService
    {
        // El editor de Unity y el juego compilado usan la misma carpeta (persistentDataPath), así que
        // cada uno tiene su propio archivo: si no, probar en el editor "gastaba" el tutorial de la build.
        private static string FilePath => Path.Combine(Application.persistentDataPath,
            Application.isEditor ? "save_editor.json" : "save.json");

        // Se carga todo una vez al arrancar y se guarda cada vez que un originador avisa que cambio.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start()
        {
            EventManager.Unsubscribe<SaveRequestedEvent>(OnSaveRequested);
            EventManager.Subscribe<SaveRequestedEvent>(OnSaveRequested);
            Load();
        }

        private static void OnSaveRequested(SaveRequestedEvent e) => Save();

        public static void Save()
        {
            var memento = new GameMemento
            {
                wallet = Denarii.CreateMemento(),
                energy = Energy.CreateMemento(),
                training = Training.CreateMemento(),
                roster = GladiatorRoster.CreateMemento(),
                blood = BloodRegistry.CreateMemento(),
            };

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(memento));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] No se pudo guardar: {e.Message}");
            }
        }

        // Si no hay archivo o esta danado, arranca una partida nueva.
        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var memento = JsonUtility.FromJson<GameMemento>(File.ReadAllText(FilePath));
                    if (memento != null)
                    {
                        Denarii.RestoreMemento(memento.wallet);
                        Energy.RestoreMemento(memento.energy);
                        Training.RestoreMemento(memento.training);
                        GladiatorRoster.RestoreMemento(memento.roster);
                        BloodRegistry.RestoreMemento(memento.blood);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Archivo ilegible, se empieza de cero: {e.Message}");
            }

            StartNewGame();
        }

        // Borra el archivo y deja todos los originadores como el primer dia.
        public static void Wipe()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] No se pudo borrar el archivo: {e.Message}");
            }

            StartNewGame();
        }

        private static void StartNewGame()
        {
            Denarii.ResetToNewGame();
            Energy.ResetToNewGame();
            Training.ResetToNewGame();
            GladiatorRoster.ResetToNewGame();
            BloodRegistry.ResetToNewGame();
        }
    }
}
