using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI.EditorTools
{
    // Herramienta de un solo uso, idempotente: agrega a la Ludus el boton y el panel de Entrenamiento
    // y completa el tutorial con combates, entrenamiento y el regalo del Novato.
    // Borrar este archivo despues de usarla.
    public static class BuildTrainingTool
    {
        private const string LudusPath = "Assets/Scenes/Ludus.unity";

        private static readonly (PortraitExpression face, string text)[] UpdateLines =
        {
            (PortraitExpression.Smile, "Ahora tocá \"Combates\": podés elegir entre Fácil, Normal o Difícil. Más rivales y más peligro pagan más denarios, y cada pelea te lleva a una zona distinta."),
            (PortraitExpression.Talk, "Con \"Entrenamiento\" mejorás a un gladiador. Mientras entrena no puede pelear, pero tranquilo: te regalé a Novato para que siempre tengas con quién salir."),
            (PortraitExpression.Calm, "El entrenamiento sigue corriendo aunque cierres el juego. Y si te impacientás, podés apurarlo pagando denarios."),
        };

        private const string NewClosingLine = "Cuando estés listo, tocá \"Combates\" y elegí tu pelea. ¡Que el público grite tu nombre!";

        [MenuItem("Tools/Insanus Pugnat/Paso 3: entrenamiento y tutorial")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Sali del Play mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string original = SceneManager.GetActiveScene().path;
            try { Build(); }
            finally
            {
                if (!string.IsNullOrEmpty(original))
                    EditorSceneManager.OpenScene(original);
            }
        }

        private static void Build()
        {
            var scene = EditorSceneManager.OpenScene(LudusPath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var controller = Object.FindFirstObjectByType<LudusController>(FindObjectsInactive.Include);
            if (canvas == null || controller == null) { Debug.LogError("[Ludus] falta Canvas o LudusController."); return; }

            bool changed = SetTutorial(controller);

            if (canvas.transform.Find("Panel_Entrenamiento") == null)
            {
                var shopButton = canvas.transform.Find("Boton_Tienda");
                if (shopButton == null) { Debug.LogError("[Ludus] no hay Boton_Tienda para copiar el estilo."); return; }

                BuildTrainingUi(canvas.transform, controller, shopButton);
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            Debug.Log(changed ? "[Ludus] entrenamiento y tutorial actualizados." : "[Ludus] ya estaba al dia.");
        }

        private static bool SetTutorial(LudusController controller)
        {
            var so = new SerializedObject(controller);
            bool changed = false;

            var intro = so.FindProperty("introDialogue");
            if (intro.arraySize > 0)
            {
                var last = intro.GetArrayElementAtIndex(intro.arraySize - 1).FindPropertyRelative("text");
                if (last.stringValue != NewClosingLine)
                {
                    last.stringValue = NewClosingLine;
                    changed = true;
                }
            }

            var update = so.FindProperty("updateDialogue");
            bool same = update.arraySize == UpdateLines.Length;
            for (int i = 0; same && i < UpdateLines.Length; i++)
                same = update.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue == UpdateLines[i].text;

            if (!same)
            {
                update.arraySize = UpdateLines.Length;
                for (int i = 0; i < UpdateLines.Length; i++)
                {
                    var line = update.GetArrayElementAtIndex(i);
                    line.FindPropertyRelative("expression").enumValueIndex = (int)UpdateLines[i].face;
                    line.FindPropertyRelative("talkWhileWriting").boolValue = true;
                    line.FindPropertyRelative("appearAgain").boolValue = false;
                    line.FindPropertyRelative("highlight").enumValueIndex = (int)DialogueHighlight.None;
                    line.FindPropertyRelative("text").stringValue = UpdateLines[i].text;
                }
                changed = true;
            }

            if (changed) so.ApplyModifiedProperties();
            return changed;
        }

        private static void BuildTrainingUi(Transform canvas, LudusController controller, Transform styleSource)
        {
            // Boton "Entrenamiento": copia del de la tienda, a la derecha del boton central.
            var openButton = Object.Instantiate(styleSource.gameObject, canvas);
            openButton.name = "Boton_Entrenamiento";
            ((RectTransform)openButton.transform).anchoredPosition = new Vector2(560f, ((RectTransform)styleSource).anchoredPosition.y);
            openButton.GetComponentInChildren<TMP_Text>().text = "Entrenamiento";
            ClearAndBind(openButton.GetComponent<Button>(), controller.ShowTraining);

            var panel = NewRect("Panel_Entrenamiento", canvas, Vector2.zero, Vector2.one);
            panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.04f, 0.96f);

            NewText("Titulo", panel, "Entrenamiento", 72, TextAlignmentOptions.Center, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.98f));
            var status = NewText("Estado", panel, "", 44, TextAlignmentOptions.Center, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.86f));
            var message = NewText("Mensaje", panel, "", 36, TextAlignmentOptions.Center, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.78f));
            message.color = new Color(1f, 0.85f, 0.3f);

            var content = NewRect("Content", panel, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.71f));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var template = BuildRowTemplate(content);

            // Boton "Apurar": copia del estilo de la tienda, arriba del boton Volver.
            var rush = Object.Instantiate(styleSource.gameObject, panel);
            rush.name = "Boton_Apurar";
            var rushRect = (RectTransform)rush.transform;
            rushRect.anchorMin = rushRect.anchorMax = new Vector2(0.5f, 0f);
            rushRect.pivot = new Vector2(0.5f, 0f);
            rushRect.sizeDelta = new Vector2(900f, 110f);
            rushRect.anchoredPosition = new Vector2(0f, 190f);
            var rushLabel = rush.GetComponentInChildren<TMP_Text>();
            rushLabel.text = "Apurar";
            for (int i = rush.GetComponent<Button>().onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(rush.GetComponent<Button>().onClick, i);

            var back = Object.Instantiate(styleSource.gameObject, panel);
            back.name = "Boton_Volver";
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(500f, 110f);
            backRect.anchoredPosition = new Vector2(0f, 40f);
            back.GetComponentInChildren<TMP_Text>().text = "Volver";
            ClearAndBind(back.GetComponent<Button>(), controller.HideTraining);

            var trainingPanel = panel.gameObject.AddComponent<TrainingPanel>();
            var panelSo = new SerializedObject(trainingPanel);
            panelSo.FindProperty("statusLabel").objectReferenceValue = status;
            panelSo.FindProperty("messageLabel").objectReferenceValue = message;
            panelSo.FindProperty("content").objectReferenceValue = content;
            panelSo.FindProperty("rowTemplate").objectReferenceValue = template;
            panelSo.FindProperty("rushButton").objectReferenceValue = rush.GetComponent<Button>();
            panelSo.FindProperty("rushLabel").objectReferenceValue = rushLabel;
            panelSo.ApplyModifiedProperties();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("trainingPanel").objectReferenceValue = trainingPanel;
            controllerSo.ApplyModifiedProperties();

            // El cuadro del instructor tiene que seguir dibujandose por encima de todo.
            var dialogue = canvas.GetComponentInChildren<TutorialDialogue>(true);
            if (dialogue != null)
            {
                int index = dialogue.transform.GetSiblingIndex();
                panel.SetSiblingIndex(index);
                openButton.transform.SetSiblingIndex(index);
            }

            panel.gameObject.SetActive(false);
        }

        private static TrainingRow BuildRowTemplate(RectTransform content)
        {
            var row = NewRect("FilaEntrenamiento", content, Vector2.zero, Vector2.one);
            row.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.13f, 0.1f, 1f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;

            var title = NewText("Titulo", row, "5 minutos", 52, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.5f), new Vector2(0.6f, 0.95f));
            title.fontStyle = FontStyles.Bold;
            var detail = NewText("Detalle", row, "+0 vida", 38, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.05f), new Vector2(0.6f, 0.5f));
            detail.color = new Color(0.85f, 0.8f, 0.7f);

            var buttonRect = NewRect("Boton_Entrenar", row, new Vector2(0.65f, 0.15f), new Vector2(0.97f, 0.85f));
            buttonRect.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.15f, 1f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            NewText("Label", buttonRect, "Entrenar", 44, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

            var trainingRow = row.gameObject.AddComponent<TrainingRow>();
            var so = new SerializedObject(trainingRow);
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("detailLabel").objectReferenceValue = detail;
            so.FindProperty("startButton").objectReferenceValue = button;
            so.ApplyModifiedProperties();
            return trainingRow;
        }

        // ---------- helpers ----------

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
