using UnityEngine;
using UnityEditor;
using Board.Input;

namespace BoardSketch.Editor
{
    public static class PlayModeTest
    {
        [MenuItem("BoardSketch/Test Gallery Flow")]
        public static void TestGalleryFlow()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[Test] Must be in play mode");
                return;
            }

            var appState = Object.FindAnyObjectByType<AppStateManager>();
            var sketchMgr = Object.FindAnyObjectByType<SketchManager>();

            if (appState == null || sketchMgr == null)
            {
                Debug.LogError("[Test] Missing AppStateManager or SketchManager");
                return;
            }

            Debug.Log("[Test] === Starting Gallery Flow Test ===");

            Debug.Log("[Test] Step 1: Opening new sketch...");
            appState.NewSketch();

            Debug.Log("[Test] Step 2: Drawing test pattern...");
            DrawTestPattern(sketchMgr);

            Debug.Log("[Test] Step 3: Saving...");
            byte[] png = sketchMgr.ExportToPNG();
            string id = SketchStorage.SaveSketch(png);
            appState.CurrentSketchId = id;
            sketchMgr.MarkClean();
            Debug.Log("[Test] Step 4: Saved id=" + id);

            appState.BackToGallery();
            var sketches = SketchStorage.ListSketches();
            Debug.Log("[Test] Step 5: Gallery has " + sketches.Count + " sketches");

            if (sketches.Count > 0)
            {
                appState.OpenSketch(sketches[0].id);
                Debug.Log("[Test] Step 6: Reopened " + sketches[0].description);
            }

            Debug.Log("[Test] === Gallery Flow Test Complete ===");
        }

        [MenuItem("BoardSketch/Test Draw Pattern")]
        public static void TestDrawPattern()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[Test] Must be in play mode");
                return;
            }

            var sketchMgr = Object.FindAnyObjectByType<SketchManager>();
            if (sketchMgr == null)
            {
                Debug.LogError("[Test] Missing SketchManager");
                return;
            }

            var appState = Object.FindAnyObjectByType<AppStateManager>();
            if (appState != null) appState.NewSketch();

            DrawTestPattern(sketchMgr);
            Debug.Log("[Test] Draw pattern complete");
        }

        [MenuItem("BoardSketch/Discover Piece IDs")]
        public static void DiscoverPieceIds()
        {
            if (!Application.isPlaying)
            {
                Debug.Log("[Test] Must be in play mode");
                return;
            }

            var glyphs = BoardInput.GetActiveContacts(BoardContactType.Glyph);
            if (glyphs.Length == 0)
            {
                Debug.Log("[Test] No glyph contacts detected");
                return;
            }

            foreach (var g in glyphs)
            {
                Debug.Log("[Test] Piece: glyphId=" + g.glyphId
                    + " orientation=" + (g.orientation * Mathf.Rad2Deg).ToString("F1") + "deg"
                    + " pos=" + g.screenPosition
                    + " touched=" + g.isTouched);
            }
        }

        private static void DrawTestPattern(SketchManager sketchMgr)
        {
            // Access the pixel buffer and texture via reflection or public API
            // Draw directly using BrushRenderer.Stamp into the canvas
            var pixels = sketchMgr.GetPixels();
            if (pixels == null)
            {
                Debug.LogError("[Test] Could not get pixel buffer");
                return;
            }

            // Rectangle
            var black = new Color32(0, 0, 0, 255);
            DrawLineOnPixels(pixels, 1920, 1080, 760, 390, 1160, 390, 8f, black);
            DrawLineOnPixels(pixels, 1920, 1080, 1160, 390, 1160, 690, 8f, black);
            DrawLineOnPixels(pixels, 1920, 1080, 1160, 690, 760, 690, 8f, black);
            DrawLineOnPixels(pixels, 1920, 1080, 760, 690, 760, 390, 8f, black);

            // Red X
            var red = new Color32(230, 50, 50, 255);
            DrawLineOnPixels(pixels, 1920, 1080, 800, 430, 1120, 650, 6f, red);
            // Blue X
            var blue = new Color32(50, 100, 230, 255);
            DrawLineOnPixels(pixels, 1920, 1080, 1120, 430, 800, 650, 6f, blue);

            sketchMgr.ApplyPixels();
        }

        private static void DrawLineOnPixels(Color32[] pixels, int w, int h,
            float x1, float y1, float x2, float y2, float size, Color32 color)
        {
            Vector2 from = new Vector2(x1, y1);
            Vector2 to = new Vector2(x2, y2);
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 1f) return;
            dir /= dist;
            float spacing = Mathf.Max(size * 0.3f, 1.5f);

            float traveled = 0;
            while (traveled <= dist)
            {
                Vector2 pos = from + dir * traveled;
                BrushRenderer.Stamp(pixels, w, h, pos, size, color);
                traveled += spacing;
            }
            BrushRenderer.Stamp(pixels, w, h, to, size, color);
        }
    }
}
