using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: HUD de economia en Menu y Ludus, tienda dentro de la Ludus,
    // texto del tutorial de la Ludus, saca el anillo de las arenas y hace que Arena 2 termine en la Ludus.
    // Solo toca lo que se nombra aca. Borrar este archivo despues de usarla.
    public static class BuildEconomyUI
    {
        private const string MenuPath = "Assets/Scenes/Menu.unity";
        private const string LudusPath = "Assets/Scenes/Ludus.unity";
        private const string Arena1Path = "Assets/Scenes/ArenaDeCombate1.unity";
        private const string Arena2Path = "Assets/Scenes/ArenaDeCombate2.unity";
        private const string ConfigPath = "Assets/Resources/EconomyConfig.asset";
        private const string ShopFolder = "Assets/Data/Shop";
        private const int Arena2Reward = 40;

        private static readonly (PortraitExpression face, string text)[] TutorialLines =
        {
            (PortraitExpression.Smile, "¡Bienvenido a la Ludus, campeón! Acá se entrenan los gladiadores que van a pelear por tu gloria."),
            (PortraitExpression.Calm, "Tocá a un gladiador para elegirlo. El que queda resaltado en amarillo es el que va a salir a la arena."),
            (PortraitExpression.Talk, "Cada uno tiene su propia vida, fuerza y velocidad de ataque. Fijate en los números de su cartel antes de decidir."),
            (PortraitExpression.Calm, "Los que se ven oscuros todavía no son tuyos. Cada vez que ganes un combate en la Arena, se suma uno nuevo, con habilidades al azar."),
            (PortraitExpression.Smile, "Con los denarios que ganes peleando, pasá por la tienda: podés reclutar gladiadores y recuperar tu energía."),
            (PortraitExpression.Aggression, "Y ojo: si tu gladiador cae en combate, pierde su entrenamiento y vuelve a sus habilidades básicas."),
            (PortraitExpression.Smile, "Cuando estés listo, tocá el botón para ir al combate. ¡Que el público grite tu nombre!"),
        };

        [MenuItem("Tools/Insanus Pugnat/Armar UI de economia")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateAssets();

            string original = SceneManager.GetActiveScene().path;
            try
            {
                BuildMenu();
                BuildLudus();
                FixArena(Arena1Path, false);
                FixArena(Arena2Path, true);
            }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        // ---------- assets ----------

        private static void CreateAssets()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Data");
            EnsureFolder(ShopFolder);

            if (AssetDatabase.LoadAssetAtPath<EconomyConfig>(ConfigPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EconomyConfig>(), ConfigPath);

            CreateSkins();
            GetOrCreateItem<RecruitGladiatorItem>("RecruitGladiator", "Recluta aleatorio", "Suma un gladiador nuevo con habilidades al azar.", 100);
            GetOrCreateItem<RefillEnergyItem>("RefillEnergy", "Recarga de energía", "Deja tu energía al máximo.", 50);
            AssetDatabase.SaveAssets();
        }

        // El orden importa: el 0 es el aspecto que ya tenia el gladiador del jugador (asi las partidas viejas no cambian).
        private static readonly string[] SkinControllers =
        {
            "Assets/Art/Animations/Gladiator_2/Gladiator_2.overrideController",
            "Assets/Art/Animations/Gladiator_Base.controller",
            "Assets/Art/Animations/Gladiator_3/Gladiator_3.overrideController",
            "Assets/Art/Animations/Amazon_1/Amazon_1.overrideController",
            "Assets/Art/Animations/Amazon_2/Amazon_2.overrideController",
            "Assets/Art/Animations/Amazon_3/Amazon_3.overrideController",
        };

        private static void CreateSkins()
        {
            const string path = "Assets/Resources/GladiatorSkins.asset";
            var skins = AssetDatabase.LoadAssetAtPath<GladiatorSkins>(path);
            if (skins == null)
            {
                skins = ScriptableObject.CreateInstance<GladiatorSkins>();
                AssetDatabase.CreateAsset(skins, path);
            }

            var so = new SerializedObject(skins);
            var list = so.FindProperty("controllers");
            list.arraySize = SkinControllers.Length;
            for (int i = 0; i < SkinControllers.Length; i++)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SkinControllers[i]);
                if (controller == null) Debug.LogError($"[Skins] no encuentro {SkinControllers[i]}.");
                list.GetArrayElementAtIndex(i).objectReferenceValue = controller;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skins);
            Debug.Log($"[Skins] {SkinControllers.Length} aspectos cargados.");
        }

        // Los items se cargan DESPUES de abrir cada escena: al abrirla Unity descarta los assets que nadie
        // referencia, y una referencia cargada antes quedaba en blanco.
        private static ShopItem[] LoadItems() => new ShopItem[]
        {
            AssetDatabase.LoadAssetAtPath<ShopItem>($"{ShopFolder}/RecruitGladiator.asset"),
            AssetDatabase.LoadAssetAtPath<ShopItem>($"{ShopFolder}/RefillEnergy.asset"),
        };

        private static void GetOrCreateItem<T>(string fileName, string title, string description, int price) where T : ShopItem
        {
            string path = $"{ShopFolder}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

            var item = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(item);
            so.FindProperty("displayName").stringValue = title;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("price").intValue = price;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(item, path);
        }

        private static void EnsureFolder(string folder)
        {
            if (Directory.Exists(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        // ---------- Menu ----------

        private static void BuildMenu()
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
            if (canvas == null || menu == null) { Debug.LogError("[Menu] falta Canvas o MenuController."); return; }

            bool changed = false;
            changed |= AddHud(canvas.transform, "HUD_Recursos", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-40f, 100f), true);
            changed |= AddWipeButton(canvas.transform, menu);

            // La tienda vive en la Ludus: se saca la que se habia puesto en el Menu.
            foreach (string name in new[] { "Boton_Tienda", "Panel_Tienda" })
            {
                var old = canvas.transform.Find(name);
                if (old == null) continue;
                Object.DestroyImmediate(old.gameObject);
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log(changed ? "[Menu] actualizado (HUD, borrar datos, sin tienda)." : "[Menu] ya estaba al dia.");
        }

        private static bool AddWipeButton(Transform canvas, MenuController menu)
        {
            var options = canvas.Find("Panel_Opciones");
            if (options == null || options.Find("Boton_BorrarDatos") != null) return false;

            var back = options.Find("Boton_Volver");
            if (back == null) { Debug.LogWarning("[Menu] sin Boton_Volver en Opciones: no se agrego Borrar datos."); return false; }

            var wipe = Object.Instantiate(back.gameObject, options);
            wipe.name = "Boton_BorrarDatos";
            ((RectTransform)wipe.transform).anchoredPosition += new Vector2(0f, 170f);
            var label = wipe.GetComponentInChildren<TMP_Text>();
            label.text = "Borrar datos";

            var button = wipe.GetComponent<Button>();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);

            var component = wipe.AddComponent<WipeDataButton>();
            var so = new SerializedObject(component);
            so.FindProperty("menu").objectReferenceValue = menu;
            so.FindProperty("label").objectReferenceValue = label;
            so.ApplyModifiedProperties();
            return true;
        }

        // ---------- Ludus ----------

        private static void BuildLudus()
        {
            var scene = EditorSceneManager.OpenScene(LudusPath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var controller = Object.FindFirstObjectByType<LudusController>(FindObjectsInactive.Include);
            if (canvas == null || controller == null) { Debug.LogError("[Ludus] falta Canvas o LudusController."); return; }

            bool changed = AddHud(canvas.transform, "HUD_Recursos", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(900f, 110f), false);

            if (canvas.transform.Find("Aviso") == null)
            {
                var notice = NewText("Aviso", canvas.transform, "", 44, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
                var rect = (RectTransform)notice.transform;
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 340f);
                rect.sizeDelta = new Vector2(1400f, 80f);
                notice.color = new Color(1f, 0.45f, 0.35f);
                notice.gameObject.SetActive(false);

                var so = new SerializedObject(controller);
                so.FindProperty("noticeLabel").objectReferenceValue = notice;
                so.ApplyModifiedProperties();
                changed = true;
            }

            changed |= AddHelpButton(canvas.transform, controller);
            changed |= AddShop(canvas.transform, controller, LoadItems());
            changed |= RepairShopItems(LoadItems());
            changed |= SetTutorialText(controller);

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log(changed ? "[Ludus] actualizada (tienda, tutorial, HUD)." : "[Ludus] ya estaba al dia.");
        }

        // Boton "?" arriba a la derecha para volver a ver el tutorial.
        private static bool AddHelpButton(Transform canvas, LudusController controller)
        {
            if (canvas.Find("Boton_Ayuda") != null) return false;

            var goButton = canvas.Find("IrAlCombateButton");
            if (goButton == null) { Debug.LogError("[Ludus] no hay IrAlCombateButton para copiar el estilo."); return false; }

            var help = Object.Instantiate(goButton.gameObject, canvas);
            help.name = "Boton_Ayuda";
            var rect = (RectTransform)help.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -20f);
            rect.sizeDelta = new Vector2(140f, 110f);
            help.GetComponentInChildren<TMP_Text>().text = "?";
            ClearAndBind(help.GetComponent<Button>(), controller.ShowHelp);

            var dialogue = canvas.GetComponentInChildren<TutorialDialogue>(true);
            if (dialogue != null)
                help.transform.SetSiblingIndex(dialogue.transform.GetSiblingIndex());
            return true;
        }

        private static bool AddShop(Transform canvas, LudusController controller, ShopItem[] items)
        {
            if (canvas.Find("Boton_Tienda") != null) return false;

            var goButton = canvas.Find("IrAlCombateButton");
            if (goButton == null) { Debug.LogError("[Ludus] no hay IrAlCombateButton para copiar el estilo."); return false; }

            // Boton "Tienda": copia del de combate, a su izquierda.
            var shopButton = Object.Instantiate(goButton.gameObject, canvas);
            shopButton.name = "Boton_Tienda";
            ((RectTransform)shopButton.transform).anchoredPosition += new Vector2(-560f, 0f);
            shopButton.GetComponentInChildren<TMP_Text>().text = "Tienda";
            ClearAndBind(shopButton.GetComponent<Button>(), controller.ShowShop);

            var panel = NewRect("Panel_Tienda", canvas, Vector2.zero, Vector2.one);
            panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.04f, 0.96f);

            NewText("Titulo", panel, "Tienda", 72, TextAlignmentOptions.Center, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.98f));
            var message = NewText("Mensaje", panel, "", 36, TextAlignmentOptions.Center, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.85f));
            message.color = new Color(1f, 0.85f, 0.3f);

            var scrollRoot = NewRect("Scroll View", panel, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.77f));
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = NewRect("Viewport", scrollRoot, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(1500f, 200f);
            grid.spacing = new Vector2(0f, 20f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var template = BuildRowTemplate(content);

            // Boton Volver: copia del de combate, centrado abajo.
            var back = Object.Instantiate(goButton.gameObject, panel);
            back.name = "Boton_Volver";
            back.GetComponentInChildren<TMP_Text>().text = "Volver";
            ClearAndBind(back.GetComponent<Button>(), controller.HideShop);

            var shop = panel.gameObject.AddComponent<ShopController>();
            var shopSo = new SerializedObject(shop);
            var list = shopSo.FindProperty("items");
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            shopSo.FindProperty("content").objectReferenceValue = content;
            shopSo.FindProperty("rowTemplate").objectReferenceValue = template;
            shopSo.FindProperty("messageLabel").objectReferenceValue = message;
            shopSo.ApplyModifiedProperties();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("shopPanel").objectReferenceValue = panel.gameObject;
            controllerSo.ApplyModifiedProperties();

            // El cuadro del instructor tiene que seguir dibujandose por encima de todo.
            var dialogue = canvas.GetComponentInChildren<TutorialDialogue>(true);
            if (dialogue != null)
            {
                int dialogueIndex = dialogue.transform.GetSiblingIndex();
                panel.SetSiblingIndex(dialogueIndex);
                shopButton.transform.SetSiblingIndex(dialogueIndex);
            }

            panel.gameObject.SetActive(false);
            return true;
        }

        // Si la tienda ya existia con items en blanco, los vuelve a asignar.
        private static bool RepairShopItems(ShopItem[] items)
        {
            var shop = Object.FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
            if (shop == null) return false;

            var so = new SerializedObject(shop);
            var list = so.FindProperty("items");
            bool fixedAny = false;
            for (int i = 0; i < list.arraySize && i < items.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null && items[i] != null)
                {
                    element.objectReferenceValue = items[i];
                    fixedAny = true;
                }
            }

            if (fixedAny) so.ApplyModifiedProperties();
            return fixedAny;
        }

        private static bool SetTutorialText(LudusController controller)
        {
            var so = new SerializedObject(controller);
            var lines = so.FindProperty("introDialogue");

            bool same = lines.arraySize == TutorialLines.Length;
            for (int i = 0; same && i < TutorialLines.Length; i++)
                same = lines.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue == TutorialLines[i].text;
            if (same) return false;

            lines.arraySize = TutorialLines.Length;
            for (int i = 0; i < TutorialLines.Length; i++)
            {
                var line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("expression").enumValueIndex = (int)TutorialLines[i].face;
                line.FindPropertyRelative("talkWhileWriting").boolValue = true;
                line.FindPropertyRelative("appearAgain").boolValue = false;
                line.FindPropertyRelative("highlight").enumValueIndex = (int)DialogueHighlight.None;
                line.FindPropertyRelative("text").stringValue = TutorialLines[i].text;
            }
            so.ApplyModifiedProperties();
            return true;
        }

        private static ShopRow BuildRowTemplate(RectTransform content)
        {
            var row = NewRect("FilaTienda", content, Vector2.zero, Vector2.one);
            row.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.13f, 0.1f, 1f);

            var name = NewText("Nombre", row, "Item", 50, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.5f), new Vector2(0.6f, 0.95f));
            name.fontStyle = FontStyles.Bold;
            var description = NewText("Descripcion", row, "Descripcion", 34, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.05f), new Vector2(0.6f, 0.5f));
            description.color = new Color(0.85f, 0.8f, 0.7f);
            var price = NewText("Precio", row, "0 denarios", 42, TextAlignmentOptions.Right, new Vector2(0.6f, 0.55f), new Vector2(0.97f, 0.95f));
            price.color = new Color(1f, 0.85f, 0.3f);

            var buyRect = NewRect("Boton_Comprar", row, new Vector2(0.65f, 0.08f), new Vector2(0.97f, 0.52f));
            buyRect.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.15f, 1f);
            var buyButton = buyRect.gameObject.AddComponent<Button>();
            NewText("Label", buyRect, "Comprar", 44, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            var shopRow = row.gameObject.AddComponent<ShopRow>();
            var so = new SerializedObject(shopRow);
            so.FindProperty("nameLabel").objectReferenceValue = name;
            so.FindProperty("descriptionLabel").objectReferenceValue = description;
            so.FindProperty("priceLabel").objectReferenceValue = price;
            so.FindProperty("buyButton").objectReferenceValue = buyButton;
            so.ApplyModifiedProperties();
            return shopRow;
        }

        // ---------- Arenas ----------

        private static void FixArena(string path, bool isArena2)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;

            if (isArena2)
            {
                var starter = Object.FindFirstObjectByType<CombatStarter>(FindObjectsInactive.Include);
                if (starter == null) { Debug.LogError($"[{scene.name}] no hay CombatStarter."); return; }

                var so = new SerializedObject(starter);
                var next = so.FindProperty("nextSceneAfterCombat");
                var reward = so.FindProperty("victoryReward");
                if (next.stringValue != "Ludus" || reward.intValue != Arena2Reward)
                {
                    next.stringValue = "Ludus";
                    reward.intValue = Arena2Reward;
                    so.ApplyModifiedProperties();
                    changed = true;
                }
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{scene.name}] {(changed ? "actualizada (sin anillo" + (isArena2 ? ", termina en la Ludus" : "") + ")" : "ya estaba al dia")}.");
        }

        // ---------- helpers ----------

        // En el Menu las dos etiquetas van en la misma linea (izquierda/derecha); en la Ludus, una arriba de la otra.
        private static bool AddHud(Transform canvas, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, bool sideBySide)
        {
            if (canvas.Find(name) != null) return false;

            var hud = NewRect(name, canvas, anchorMin, anchorMax);
            hud.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, 1f);
            hud.anchoredPosition = position;
            hud.sizeDelta = size;

            TMP_Text denarii, energy;
            if (sideBySide)
            {
                denarii = NewText("Denarios", hud, "0 denarios", 40, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(0.4f, 1f));
                energy = NewText("Energia", hud, "Energía", 40, TextAlignmentOptions.Right, new Vector2(0.4f, 0f), new Vector2(1f, 1f));
            }
            else
            {
                denarii = NewText("Denarios", hud, "0 denarios", 40, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(1f, 1f));
                energy = NewText("Energia", hud, "Energía", 36, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0.5f));
            }

            var component = hud.gameObject.AddComponent<ResourceHud>();
            var so = new SerializedObject(component);
            so.FindProperty("denariiLabel").objectReferenceValue = denarii;
            so.FindProperty("energyLabel").objectReferenceValue = energy;
            so.ApplyModifiedProperties();
            return true;
        }

        private static void ClearAndBind(Button button, UnityEngine.Events.UnityAction action)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = NewRect(name, parent, anchorMin, anchorMax);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
