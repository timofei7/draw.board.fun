using UnityEngine;
using UnityEditor;
using System.Collections;
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
            var gallery = Object.FindAnyObjectByType<GalleryController>();

            if (appState == null || sketchMgr == null)
            {
                Debug.LogError("[Test] Missing AppStateManager or SketchManager");
                return;
            }

            Debug.Log("[Test] === Starting Gallery Flow Test ===");

            // Step 1: We should be on gallery screen
            Debug.Log("[Test] Step 1: Gallery should be visible. Opening new sketch...");
            appState.NewSketch();
            Debug.Log("[Test] Step 2: Canvas should now be visible. Drawing test strokes...");

            // Step 2: Draw some strokes programmatically
            DrawTestPattern(sketchMgr);
            Debug.Log("[Test] Step 3: Test pattern drawn. IsDirty=" + sketchMgr.IsDirty);

            // Step 3: Save
            byte[] png = sketchMgr.ExportToPNG();
            string id = SketchStorage.SaveSketch(png);
            appState.CurrentSketchId = id;
            sketchMgr.MarkClean();
            Debug.Log("[Test] Step 4: Saved sketch id=" + id);

            // Step 4: Back to gallery
            appState.BackToGallery();
            var sketches = SketchStorage.ListSketches();
            Debug.Log("[Test] Step 5: Back to gallery. Saved sketches count=" + sketches.Count);

            // Step 5: Reopen the sketch
            if (sketches.Count > 0)
            {
                appState.OpenSketch(sketches[0].id);
                Debug.Log("[Test] Step 6: Reopened sketch id=" + sketches[0].id + " desc=" + sketches[0].description);
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

            // Make sure we're on canvas
            var appState = Object.FindAnyObjectByType<AppStateManager>();
            if (appState != null) appState.NewSketch();

            DrawTestPattern(sketchMgr);
            Debug.Log("[Test] Draw pattern complete. IsDirty=" + sketchMgr.IsDirty);
        }

        private static void DrawTestPattern(SketchManager sketchMgr)
        {
            var drawRT = sketchMgr.DrawRT;
            var brushMat = new Material(Shader.Find("BoardSketch/BrushStamp"));

            // Draw a rectangle in the center
            float cx = 960, cy = 540;
            float w = 400, h = 300;
            Color color = Color.black;
            float size = 8f;

            // Top edge
            DrawLine(drawRT, brushMat, cx - w / 2, cy + h / 2, cx + w / 2, cy + h / 2, color, size);
            // Right edge
            DrawLine(drawRT, brushMat, cx + w / 2, cy + h / 2, cx + w / 2, cy - h / 2, color, size);
            // Bottom edge
            DrawLine(drawRT, brushMat, cx + w / 2, cy - h / 2, cx - w / 2, cy - h / 2, color, size);
            // Left edge
            DrawLine(drawRT, brushMat, cx - w / 2, cy - h / 2, cx - w / 2, cy + h / 2, color, size);

            // Draw an X inside
            DrawLine(drawRT, brushMat, cx - w / 3, cy + h / 3, cx + w / 3, cy - h / 3, new Color(0.9f, 0.2f, 0.2f), 6f);
            DrawLine(drawRT, brushMat, cx + w / 3, cy + h / 3, cx - w / 3, cy - h / 3, new Color(0.2f, 0.4f, 0.9f), 6f);

            Object.Destroy(brushMat);
        }

        private static void DrawLine(RenderTexture rt, Material mat, float x1, float y1, float x2, float y2, Color color, float size)
        {
            // These are already in RT coords (1920x1080), stamp directly
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, 1920, 1080, 0);

            mat.SetColor("_Color", color);
            mat.SetTexture("_BrushTex", BrushRenderer.GetDefaultBrushTexture());
            mat.SetPass(0);

            float spacing = Mathf.Max(size * 0.25f, 1f);
            Vector2 from = new Vector2(x1, y1);
            Vector2 to = new Vector2(x2, y2);
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            dir /= dist;

            float traveled = 0;
            while (traveled <= dist)
            {
                Vector2 pos = from + dir * traveled;
                float half = size / 2f;
                GL.Begin(GL.QUADS);
                GL.TexCoord2(0, 0); GL.Vertex3(pos.x - half, pos.y - half, 0);
                GL.TexCoord2(1, 0); GL.Vertex3(pos.x + half, pos.y - half, 0);
                GL.TexCoord2(1, 1); GL.Vertex3(pos.x + half, pos.y + half, 0);
                GL.TexCoord2(0, 1); GL.Vertex3(pos.x - half, pos.y + half, 0);
                GL.End();
                traveled += spacing;
            }

            GL.PopMatrix();
            RenderTexture.active = null;
        }
    }
}
