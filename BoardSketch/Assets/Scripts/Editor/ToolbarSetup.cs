using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardSketch.Editor
{
    public static class ToolbarSetup
    {
        [MenuItem("BoardSketch/Setup Toolbar UI")]
        public static void Setup()
        {
            var canvasGO = new GameObject("UICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var toolbarGO = CreateUI("Toolbar", canvasGO.transform);
            var toolbarRect = toolbarGO.GetComponent<RectTransform>();
            toolbarRect.anchorMin = new Vector2(0.5f, 0f);
            toolbarRect.anchorMax = new Vector2(0.5f, 0f);
            toolbarRect.pivot = new Vector2(0.5f, 0f);
            toolbarRect.anchoredPosition = new Vector2(0, 20);
            toolbarRect.sizeDelta = new Vector2(1200, 70);

            var toolbarImg = toolbarGO.AddComponent<Image>();
            toolbarImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            var layout = toolbarGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var blackBtn = MakeColorBtn("BlackBtn", toolbarGO.transform, Color.black);
            var redBtn = MakeColorBtn("RedBtn", toolbarGO.transform, new Color(0.9f, 0.2f, 0.2f));
            var blueBtn = MakeColorBtn("BlueBtn", toolbarGO.transform, new Color(0.2f, 0.4f, 0.9f));
            var greenBtn = MakeColorBtn("GreenBtn", toolbarGO.transform, new Color(0.2f, 0.8f, 0.3f));
            var yellowBtn = MakeColorBtn("YellowBtn", toolbarGO.transform, new Color(0.95f, 0.8f, 0.1f));
            var eraserBtn = MakeColorBtn("EraserBtn", toolbarGO.transform, Color.white);

            MakeSep(toolbarGO.transform);

            var smallBtn = MakeSizeBtn("SmallBtn", toolbarGO.transform, 8f);
            var mediumBtn = MakeSizeBtn("MediumBtn", toolbarGO.transform, 18f);
            var largeBtn = MakeSizeBtn("LargeBtn", toolbarGO.transform, 30f);

            MakeSep(toolbarGO.transform);

            var undoBtn = MakeActionBtn("UndoBtn", toolbarGO.transform, "\u21A9");
            var clearBtn = MakeActionBtn("ClearBtn", toolbarGO.transform, "\u2715");
            var saveBtn = MakeActionBtn("SaveBtn", toolbarGO.transform, "\u2193");

            MakeSep(toolbarGO.transform);

            var colorInd = CreateUI("ColorIndicator", toolbarGO.transform);
            colorInd.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54);
            var colorIndImg = colorInd.AddComponent<Image>();
            colorIndImg.color = Color.black;

            var sizeInd = CreateUI("SizeIndicator", toolbarGO.transform);
            sizeInd.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
            var sizeIndImg = sizeInd.AddComponent<Image>();
            sizeIndImg.color = Color.white;

            var controller = toolbarGO.AddComponent<ToolbarController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_sketchManager").objectReferenceValue = Object.FindFirstObjectByType<SketchManager>();
            so.FindProperty("_blackBtn").objectReferenceValue = blackBtn.GetComponent<Button>();
            so.FindProperty("_redBtn").objectReferenceValue = redBtn.GetComponent<Button>();
            so.FindProperty("_blueBtn").objectReferenceValue = blueBtn.GetComponent<Button>();
            so.FindProperty("_greenBtn").objectReferenceValue = greenBtn.GetComponent<Button>();
            so.FindProperty("_yellowBtn").objectReferenceValue = yellowBtn.GetComponent<Button>();
            so.FindProperty("_eraserBtn").objectReferenceValue = eraserBtn.GetComponent<Button>();
            so.FindProperty("_smallBtn").objectReferenceValue = smallBtn.GetComponent<Button>();
            so.FindProperty("_mediumBtn").objectReferenceValue = mediumBtn.GetComponent<Button>();
            so.FindProperty("_largeBtn").objectReferenceValue = largeBtn.GetComponent<Button>();
            so.FindProperty("_undoBtn").objectReferenceValue = undoBtn.GetComponent<Button>();
            so.FindProperty("_clearBtn").objectReferenceValue = clearBtn.GetComponent<Button>();
            so.FindProperty("_saveBtn").objectReferenceValue = saveBtn.GetComponent<Button>();
            so.FindProperty("_colorIndicator").objectReferenceValue = colorIndImg;
            so.FindProperty("_sizeIndicator").objectReferenceValue = sizeIndImg;
            so.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Toolbar UI");
            Debug.Log("[BoardSketch] Toolbar UI created!");
        }

        static GameObject CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject MakeColorBtn(string name, Transform parent, Color color)
        {
            var go = CreateUI(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = color * 0.8f;
            c.pressedColor = color * 0.6f;
            c.highlightedColor = new Color(c.highlightedColor.r, c.highlightedColor.g, c.highlightedColor.b, 1f);
            c.pressedColor = new Color(c.pressedColor.r, c.pressedColor.g, c.pressedColor.b, 1f);
            btn.colors = c;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            return go;
        }

        static GameObject MakeSizeBtn(string name, Transform parent, float dotSize)
        {
            var go = CreateUI(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            go.AddComponent<Button>();
            var dot = CreateUI("Dot", go.transform);
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(dotSize, dotSize);
            dot.AddComponent<Image>().color = Color.white;
            return go;
        }

        static GameObject MakeActionBtn(string name, Transform parent, string label)
        {
            var go = CreateUI(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 54);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            go.AddComponent<Button>();
            var textGO = CreateUI("Label", go.transform);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        static void MakeSep(Transform parent)
        {
            var go = CreateUI("Sep", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 40);
            go.AddComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        }
    }
}
