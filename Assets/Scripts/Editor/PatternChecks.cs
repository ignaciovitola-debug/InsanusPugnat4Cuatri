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
            CheckRemoteTuning();
            CheckTrainingOutcome();
            CheckFrenzy();
            CheckAds();

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
            GladiatorRoster.ApplyTraining(1, new TrainingOutcome(10f, 2f, false, true));
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
            Check("Memento restaura la guardia entrenada", GladiatorRoster.Slots[1].guardLevel == 1);
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

        private static void CheckTrainingOutcome()
        {
            var tier = new EconomyConfig.TrainingTier { minutes = 5, bonusHP = 5f, bonusDamage = 2f, doubleChance = 0.25f, guardChance = 0.15f };

            var plain = TrainingOutcome.Roll(tier, 0.99f, 0.99f);
            Check("Entrenar da +5 de vida y +2 de ataque", Near(plain.bonusHP, 5f) && Near(plain.bonusDamage, 2f));
            Check("Sin suerte no hay doble ni guardia", !plain.doubled && !plain.guardUp);

            var lucky = TrainingOutcome.Roll(tier, 0.1f, 0.1f);
            Check("Con suerte sale doble (+10 y +4)", lucky.doubled && Near(lucky.bonusHP, 10f) && Near(lucky.bonusDamage, 4f));
            Check("Con suerte sale mejora de guardia", lucky.guardUp);

            var cfg = EconomyConfig.Instance.trainingTiers;
            bool flat = true;
            foreach (var t in cfg) flat &= Near(t.bonusHP, 5f) && Near(t.bonusDamage, 2f);
            Check("Los 3 entrenamientos dan lo mismo (+5 y +2)", cfg.Length == 3 && flat);
            Check("Chances de doble de 5 en 5 (5, 10, 15%)", Near(cfg[0].doubleChance, 0.05f) && Near(cfg[1].doubleChance, 0.10f) && Near(cfg[2].doubleChance, 0.15f));
            Check("Chances de guardia de 5 en 5 (5, 10, 15%)", Near(cfg[0].guardChance, 0.05f) && Near(cfg[1].guardChance, 0.10f) && Near(cfg[2].guardChance, 0.15f));

            StartNew();
            float hpBefore = GladiatorRoster.Slots[1].maxHP;
            GladiatorRoster.ApplyTraining(1, lucky);
            var trained = GladiatorRoster.Slots[1];
            Check("Aplicar el entrenamiento sube vida, nivel y guardia", Near(trained.maxHP, hpBefore + 10f) && trained.level == 1 && trained.guardLevel == 1);
            GladiatorRoster.ResetSlotToBasic(1);
            Check("Caer en combate pierde lo entrenado y la guardia", GladiatorRoster.Slots[1].guardLevel == 0 && GladiatorRoster.Slots[1].level == 0
                && Near(GladiatorRoster.Slots[1].maxHP, hpBefore));
            StartNew();
        }

        private static void CheckAds()
        {
            var config = AdsConfig.Instance;
            StartNew();
            Energy.TrySpend(3);
            AdRewards.Grant(AdSlot.Energy, AdOutcome.Skipped, config);
            Check("Anuncio de energia cerrado antes da poca energia", Energy.Current == Energy.Max - 3 + config.energyIfSkipped);
            AdRewards.Grant(AdSlot.Energy, AdOutcome.Completed, config);
            Check("Anuncio de energia entero llena la energia", Energy.Current == Energy.Max);

            StartNew();
            AdRewards.Grant(AdSlot.Coins, AdOutcome.Skipped, config);
            Check("Anuncio de monedas cerrado antes da poco", Denarii.Balance == config.coinsIfSkipped);
            AdRewards.Grant(AdSlot.Coins, AdOutcome.Completed, config);
            Check("Anuncio de monedas entero da el premio completo", Denarii.Balance == config.coinsIfSkipped + config.coinsIfCompleted);
            Check("El premio entero es mayor que el de cerrarlo antes", config.coinsIfCompleted > config.coinsIfSkipped);

            int before = Denarii.Balance;
            string failMessage = AdRewards.Grant(AdSlot.Coins, AdOutcome.Failed, config);
            Check("Si el anuncio falla no se da premio", Denarii.Balance == before && !string.IsNullOrEmpty(failMessage));

            var item = ScriptableObject.CreateInstance<WatchAdItem>();
            Check("El item del anuncio es gratis", item.PriceText == "Gratis");
            Check("Con la energia llena no se ofrece el anuncio", !item.CanBuy(out _));
            Energy.TrySpend(2);
            Check("Sin anuncio cargado tampoco se puede comprar", !item.CanBuy(out string reason) && !string.IsNullOrEmpty(reason));
            UnityEngine.Object.DestroyImmediate(item);

            // El Presenter muestra en la vista el mensaje del premio cuando termina un anuncio.
            var view = new FakeView();
            var presenter = new ShopPresenter(view, new ShopModel(new ShopItem[0]));
            EventManager.Raise(new AdFinishedEvent(AdSlot.Coins, AdOutcome.Completed, "mensaje de prueba"));
            Check("La tienda muestra el premio del anuncio", view.lastMessage == "mensaje de prueba");
            presenter.Dispose();
            StartNew();
        }

        private static void CheckFrenzy()
        {
            var combat = new GladiatorCombat(10f, 20f, 1f, 0.4f);
            combat.RegisterAttack();
            combat.Tick(0.6f);
            Check("Sin bufo el golpe espera el cooldown completo", combat.IsOnCooldown);

            combat = new GladiatorCombat(10f, 20f, 1f, 0.4f);
            Check("La orden Atacar activa el bufo", combat.TryRedoubleAttack(3f, 0.5f) && combat.IsFrenzied);
            combat.RegisterAttack();
            combat.Tick(0.6f);
            Check("Con bufo golpea mas seguido", !combat.IsOnCooldown);
            Check("No se puede dar otra orden enseguida", !combat.TryRedoubleAttack(3f, 0.5f));

            combat.Tick(3f);
            Check("El bufo termina solo", !combat.IsFrenzied);
            combat.RegisterAttack();
            combat.Tick(0.6f);
            Check("Despues del bufo vuelve el cooldown normal", combat.IsOnCooldown);
        }

        private static void CheckRemoteTuning()
        {
            RemoteTuning.ResetToDefaults();
            int localMax = EconomyConfig.Instance.maxEnergy;
            Check("Remote sin datos usa los valores locales", RemoteTuning.MaxEnergy == localMax && Near(RemoteTuning.RewardMultiplier, 1f));

            RemoteTuning.SetMaxEnergy(8);
            RemoteTuning.SetRewardMultiplier(2f);
            RemoteTuning.SetRushCostPerMinute(9);
            RemoteTuning.SetMinutesPerEnergy(3f);
            Check("Remote pisa los valores", RemoteTuning.MaxEnergy == 8 && Near(RemoteTuning.RewardMultiplier, 2f)
                && RemoteTuning.RushCostPerMinute == 9 && Near(RemoteTuning.MinutesPerEnergy, 3f));
            Check("Energy usa el maximo remoto", Energy.Max == 8);

            RemoteTuning.SetMaxEnergy(9999);
            RemoteTuning.SetRewardMultiplier(-5f);
            RemoteTuning.SetMinutesPerEnergy(0f);
            Check("Remote limita valores fuera de rango", RemoteTuning.MaxEnergy == 20 && Near(RemoteTuning.RewardMultiplier, 0f)
                && Near(RemoteTuning.MinutesPerEnergy, 0.1f));

            RemoteTuning.ResetToDefaults();
            Check("Remote vuelve a los locales", RemoteTuning.MaxEnergy == localMax);
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
