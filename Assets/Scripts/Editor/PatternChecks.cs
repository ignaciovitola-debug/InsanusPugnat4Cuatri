using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladiusAI.EditorTools
{
    // Herramienta temporal: comprueba los patrones y escribe PASS/FAIL en el log.
    public static class PatternChecks
    {
        private static int failures;

        private class FakeView : IShopView
        {
            public event Action<int> BuyRequested;
            public int offersShown;
            public int sounds;
            public string lastMessage;

            public void ShowOffers(IReadOnlyList<ShopOffer> offers) => offersShown = offers.Count;
            public void ShowMessage(string text) => lastMessage = text;
            public void PlayPurchaseSound() => sounds++;
            public void Press(int index) => BuyRequested?.Invoke(index);
        }

        [MenuItem("Tools/Temp/Run Pattern Checks")]
        public static void Run()
        {
            failures = 0;
            CheckLookUpTable();
            CheckStrategies();
            CheckMemento();
            CheckShopPresenter();

            Debug.Log(failures == 0 ? "[Checks] TODO PASS" : $"[Checks] FALLARON {failures}");
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        private static void Check(string name, bool condition)
        {
            if (!condition) failures++;
            Debug.Log($"[Checks] {(condition ? "PASS" : "FAIL")} {name}");
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.001f;

        private static void CheckLookUpTable()
        {
            var table = new LookUpTable(x => 2f * x, 0f, 10f, 11);
            Check("LUT valor exacto en una muestra", Near(table.ReturnValue(5f), 10f));
            Check("LUT interpola entre muestras", Near(table.ReturnValue(2.5f), 5f));
            Check("LUT enemigo mas fuerte con mas nivel", ProgressionTables.EnemyScaleForLevel(10) > ProgressionTables.EnemyScaleForLevel(0));
            Check("LUT entrenamiento rinde menos con mas nivel", ProgressionTables.TrainingBonusForLevel(10) < ProgressionTables.TrainingBonusForLevel(0));
        }

        private static void CheckStrategies()
        {
            var list = EconomyConfig.Instance.difficulties;
            Check("Strategy hay 3 dificultades", list.Length == 3);

            var counts = new HashSet<int>();
            var rewards = new HashSet<int>();
            var hp = new HashSet<float>();
            var baseStats = new CombatStats(100f, 10f, 20f, 1.2f);
            foreach (var d in list)
            {
                counts.Add(d.EnemyCount);
                rewards.Add(d.Reward());
                hp.Add(d.Scale(baseStats, 1.5f).maxHP);
            }
            Check("Strategy cantidades de rivales distintas", counts.Count == list.Length);
            Check("Strategy recompensas distintas", rewards.Count == list.Length);
            Check("Strategy escalan distinto la vida", hp.Count == list.Length);
        }

        private static void CheckMemento()
        {
            StartNew();
            Denarii.Add(321);
            Energy.TrySpend(2);
            Training.TryStart(0, 0, out _);
            GladiatorRoster.UnlockNext();
            GladiatorRoster.AddTrainingBonus(1, 10f, 2f);
            GladiatorRoster.LudusTutorialVersion = 3;
            GladiatorRoster.ArenaTutorialDone = true;

            var before = Snapshot();
            var json = JsonUtility.ToJson(before);
            StartNew();
            Check("Memento reinicio deja 0 denarios", Denarii.Balance == 0);
            Check("Memento reinicio deja entrenamiento libre", !Training.IsActive);

            var after = JsonUtility.FromJson<GameMemento>(json);
            Denarii.RestoreMemento(after.wallet);
            Energy.RestoreMemento(after.energy);
            Training.RestoreMemento(after.training);
            GladiatorRoster.RestoreMemento(after.roster);

            Check("Memento restaura denarios", Denarii.Balance == 321);
            Check("Memento restaura energia", Energy.Current == Energy.Max - 2 || Energy.Current == Energy.Max - 1);
            Check("Memento restaura entrenamiento", Training.IsActive && Training.Slot == 0);
            Check("Memento restaura tutorial", GladiatorRoster.LudusTutorialVersion == 3);
            Check("Memento restaura tutorial de la Arena 1 completado", GladiatorRoster.ArenaTutorialDone);
            Check("Memento restaura nivel del gladiador", GladiatorRoster.Slots[1].level == 1);
            Check("Memento ida y vuelta identica", json == JsonUtility.ToJson(Snapshot()));

            StartNew();
            Check("Wipe vuelve a pedir el tutorial", !GladiatorRoster.ArenaTutorialDone);
            Check("Wipe deja el roster inicial",GladiatorRoster.Slots[1].unlocked && !GladiatorRoster.Slots[2].unlocked && GladiatorRoster.Slots[1].level == 0);
        }

        private static GameMemento Snapshot() => new GameMemento
        {
            wallet = Denarii.CreateMemento(),
            energy = Energy.CreateMemento(),
            training = Training.CreateMemento(),
            roster = GladiatorRoster.CreateMemento(),
        };

        private static void StartNew()
        {
            Denarii.ResetToNewGame();
            Energy.ResetToNewGame();
            Training.ResetToNewGame();
            GladiatorRoster.ResetToNewGame();
        }

        private static void CheckShopPresenter()
        {
            StartNew();
            var refill = ScriptableObject.CreateInstance<RefillEnergyItem>();
            var view = new FakeView();
            var presenter = new ShopPresenter(view, new ShopModel(new ShopItem[] { refill }));
            Check("MVP la vista recibe las ofertas", view.offersShown == 1);

            Energy.TrySpend(1);
            view.Press(0);
            Check("MVP sin plata rechaza", view.lastMessage != null && view.lastMessage.StartsWith("Te faltan") && view.sounds == 0);

            Denarii.Add(refill.Price + 50);
            view.Press(0);
            Check("MVP con plata compra, cobra y suena", view.sounds == 1 && Denarii.Balance == 50 && Energy.Current == Energy.Max);

            view.Press(0);
            Check("MVP item no disponible no cobra", Denarii.Balance == 50 && view.sounds == 1);

            presenter.Dispose();
            view.Press(0);
            Check("MVP Dispose desconecta la vista", view.sounds == 1);

            UnityEngine.Object.DestroyImmediate(refill);
            StartNew();
        }
    }
}
