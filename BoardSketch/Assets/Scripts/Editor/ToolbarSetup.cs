using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace BoardSketch.Editor
{
    public static class ToolbarSetup
    {
        [MenuItem("BoardSketch/Setup Full UI")]
        public static void SetupFullUI()
        {
            // Delete existing UICanvas if present
            var existing = GameObject.Find("UICanvas");
            if (existing) Undo.DestroyObjectImmediate(existing);

            var canvasGO = new GameObject("UICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // --- Gallery View (shown on startup) ---
            var galleryView = CreateUI("GalleryView", canvasGO.transform);
            var galleryRect = galleryView.GetComponent<RectTransform>();
            galleryRect.anchorMin = Vector2.zero;
            galleryRect.anchorMax = Vector2.one;
            galleryRect.sizeDelta = Vector2.zero;

            var galleryBg = galleryView.AddComponent<Image>();
            galleryBg.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            // Header
            var header = CreateUI("Header", galleryView.transform);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.85f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Title
            var title = CreateUI("Title", header.transform);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.offsetMin = new Vector2(40, 0);
            titleRect.offsetMax = Vector2.zero;
            var titleText = title.AddComponent<Text>();
            titleText.text = "BoardSketch";
            titleText.fontSize = 42;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // New Sketch button
            var newBtn = CreateUI("NewSketchBtn", header.transform);
            var newBtnRect = newBtn.GetComponent<RectTransform>();
            newBtnRect.anchorMin = new Vector2(1, 0.5f);
            newBtnRect.anchorMax = new Vector2(1, 0.5f);
            newBtnRect.pivot = new Vector2(1, 0.5f);
            newBtnRect.anchoredPosition = new Vector2(-40, 0);
            newBtnRect.sizeDelta = new Vector2(220, 60);

            newBtn.AddComponent<Image>().color = new Color(0.2f, 0.6f, 1f, 1f);
            newBtn.AddComponent<Button>();

            var newLabel = CreateUI("Label", newBtn.transform);
            var newLabelRect = newLabel.GetComponent<RectTransform>();
            newLabelRect.anchorMin = Vector2.zero;
            newLabelRect.anchorMax = Vector2.one;
            newLabelRect.sizeDelta = Vector2.zero;
            var newLabelText = newLabel.AddComponent<Text>();
            newLabelText.text = "+ New Sketch";
            newLabelText.fontSize = 26;
            newLabelText.alignment = TextAnchor.MiddleCenter;
            newLabelText.color = Color.white;
            newLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Scroll area with grid
            var scrollArea = CreateUI("ScrollArea", galleryView.transform);
            var scrollRect = scrollArea.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = new Vector2(1, 0.85f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.offsetMin = new Vector2(40, 40);
            scrollRect.offsetMax = new Vector2(-40, -20);

            var gridContainer = CreateUI("GridContainer", scrollArea.transform);
            var gridRect = gridContainer.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.pivot = new Vector2(0.5f, 1);
            gridRect.sizeDelta = new Vector2(0, 800);

            var grid = gridContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280, 180);
            grid.spacing = new Vector2(24, 24);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            var csf = gridContainer.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollArea.AddComponent<ScrollRect>();
            scroll.content = gridRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Gallery Controller
            var galleryCtrl = galleryView.AddComponent<GalleryController>();
            var gallerySO = new SerializedObject(galleryCtrl);
            gallerySO.FindProperty("_appState").objectReferenceValue = Object.FindAnyObjectByType<AppStateManager>();
            gallerySO.FindProperty("_gridContainer").objectReferenceValue = gridContainer.transform;
            gallerySO.FindProperty("_newSketchBtn").objectReferenceValue = newBtn.GetComponent<Button>();
            gallerySO.ApplyModifiedProperties();

            // --- Canvas View (toolbar, hidden on startup) ---
            var canvasView = CreateUI("CanvasView", canvasGO.transform);
            var canvasViewRect = canvasView.GetComponent<RectTransform>();
            canvasViewRect.anchorMin = Vector2.zero;
            canvasViewRect.anchorMax = Vector2.one;
            canvasViewRect.sizeDelta = Vector2.zero;

            // Toolbar panel
            var toolbarGO = CreateUI("Toolbar", canvasView.transform);
            var toolbarRect = toolbarGO.GetComponent<RectTransform>();
            toolbarRect.anchorMin = new Vector2(0.5f, 0f);
            toolbarRect.anchorMax = new Vector2(0.5f, 0f);
            toolbarRect.pivot = new Vector2(0.5f, 0f);
            toolbarRect.anchoredPosition = new Vector2(0, 20);
            toolbarRect.sizeDelta = new Vector2(1300, 70);

            var toolbarImg = toolbarGO.AddComponent<Image>();
            toolbarImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            var layout = toolbarGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Back button
            var backBtn = MakeActionBtn("BackBtn", toolbarGO.transform, "\u2190");

            MakeSep(toolbarGO.transform);

            // Color buttons
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

            // Wire ToolbarController
            var controller = toolbarGO.AddComponent<ToolbarController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_sketchManager").objectReferenceValue = Object.FindAnyObjectByType<SketchManager>();
            so.FindProperty("_backBtn").objectReferenceValue = backBtn.GetComponent<Button>();
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

            // Wire AppStateManager
            var appState = Object.FindAnyObjectByType<AppStateManager>();
            if (appState)
            {
                var appSO = new SerializedObject(appState);
                appSO.FindProperty("_sketchManager").objectReferenceValue = Object.FindAnyObjectByType<SketchManager>();
                appSO.FindProperty("_galleryView").objectReferenceValue = galleryView;
                appSO.FindProperty("_canvasView").objectReferenceValue = canvasView;
                appSO.ApplyModifiedProperties();
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Full UI");
            Debug.Log("[BoardSketch] Full UI created (gallery + toolbar)!");
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

        [MenuItem("BoardSketch/Create Piece Config")]
        public static void CreatePieceConfig()
        {
            var config = ScriptableObject.CreateInstance<PieceToolConfig>();
            config.dials = new PieceToolConfig.PieceDial[]
            {
                new PieceToolConfig.PieceDial
                {
                    glyphId = 5,
                    label = "Ship Yellow - Color Wheel",
                    dialType = PieceDialType.ColorWheel
                },
                new PieceToolConfig.PieceDial
                {
                    glyphId = 6,
                    label = "Ship Purple - Size Dial",
                    dialType = PieceDialType.BrushSize,
                    minValue = 2f,
                    maxValue = 40f
                },
                new PieceToolConfig.PieceDial
                {
                    glyphId = 7,
                    label = "Ship Orange - Eraser",
                    dialType = PieceDialType.Eraser
                }
            };

            if (!AssetDatabase.IsValidFolder("Assets/Config"))
                AssetDatabase.CreateFolder("Assets", "Config");

            AssetDatabase.CreateAsset(config, "Assets/Config/ArcadePieceToolConfig.asset");
            AssetDatabase.SaveAssets();

            // Wire to SketchManager
            var sketchMgr = Object.FindAnyObjectByType<SketchManager>();
            if (sketchMgr != null)
            {
                var so = new SerializedObject(sketchMgr);
                so.FindProperty("_pieceToolConfig").objectReferenceValue = config;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[BoardSketch] Piece config created: Yellow(5)=ColorWheel, Purple(6)=SizeDial, Orange(7)=Eraser");
        }
    }
}
