using UnityEngine;

namespace BoardSketch
{
    public static class BrushRenderer
    {
        private static Texture2D _defaultBrushTex;

        public static Texture2D GetDefaultBrushTexture()
        {
            if (_defaultBrushTex != null) return _defaultBrushTex;

            int size = 64;
            _defaultBrushTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.0f, 1.0f, dist));
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            _defaultBrushTex.SetPixels(pixels);
            _defaultBrushTex.Apply();
            _defaultBrushTex.wrapMode = TextureWrapMode.Clamp;
            return _defaultBrushTex;
        }

        public static void StampAlongPath(
            RenderTexture target,
            Material brushMat,
            Vector2 from,
            Vector2 to,
            Color color,
            float size,
            ref Vector2 lastStampPos)
        {
            float spacing = Mathf.Max(size * 0.25f, 1f);
            Vector2 dir = to - lastStampPos;
            float dist = dir.magnitude;

            if (dist < spacing)
                return;

            dir /= dist;

            BeginStamping(target, brushMat, color);

            float traveled = spacing;
            while (traveled <= dist)
            {
                Vector2 pos = lastStampPos + dir * traveled;
                StampQuad(pos, size);
                traveled += spacing;
            }

            EndStamping();
            lastStampPos = to;
        }

        public static void StampSingle(
            RenderTexture target,
            Material brushMat,
            Vector2 position,
            Color color,
            float size)
        {
            BeginStamping(target, brushMat, color);
            StampQuad(position, size);
            EndStamping();
        }

        public static void StampAlongPathSmooth(
            RenderTexture target,
            Material brushMat,
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            Color color,
            float size,
            ref Vector2 lastStampPos)
        {
            float spacing = Mathf.Max(size * 0.25f, 1f);

            float chordLength = (p2 - p1).magnitude;
            float controlLength = (p1 - p0).magnitude + (p2 - p1).magnitude + (p3 - p2).magnitude;
            float estimatedLength = (chordLength + controlLength) / 2f;
            int steps = Mathf.Max(Mathf.CeilToInt(estimatedLength / spacing), 1);
            float tStep = 1f / steps;

            BeginStamping(target, brushMat, color);

            for (int i = 0; i <= steps; i++)
            {
                float t = i * tStep;
                Vector2 pos = CatmullRom(p0, p1, p2, p3, t);

                if ((pos - lastStampPos).sqrMagnitude >= spacing * spacing)
                {
                    StampQuad(pos, size);
                    lastStampPos = pos;
                }
            }

            EndStamping();
            lastStampPos = p2;
        }

        /// <summary>
        /// Convert Board SDK screenPosition (screen pixels, Y-down) to
        /// RenderTexture pixel coordinates via Camera projection.
        /// This properly handles Game View aspect ratios that differ from 16:9.
        /// </summary>
        public static Vector2 ScreenToRT(Vector2 screenPosition, Camera cam)
        {
            // SDK uses Y-down; Camera.ScreenToWorldPoint expects Y-up
            float screenY = Screen.height - screenPosition.y;
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenY, 11f));

            // Quad spans (-9.6, -5.4) to (9.6, 5.4) in world space = 19.2 x 10.8
            float rtX = (worldPos.x + 9.6f) / 19.2f * 1920f;
            float rtY = (worldPos.y + 5.4f) / 10.8f * 1080f;

            return new Vector2(rtX, rtY);
        }

        private static void BeginStamping(RenderTexture target, Material brushMat, Color color)
        {
            RenderTexture.active = target;
            GL.PushMatrix();
            // Y-down (1080 at bottom, 0 at top) to compensate for RT display flip on Quad
            GL.LoadPixelMatrix(0, 1920, 1080, 0);

            brushMat.SetColor("_Color", color);
            brushMat.SetTexture("_BrushTex", GetDefaultBrushTexture());
            brushMat.SetPass(0);
        }

        private static void EndStamping()
        {
            GL.PopMatrix();
            RenderTexture.active = null;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static void StampQuad(Vector2 center, float size)
        {
            float half = size / 2f;
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0); GL.Vertex3(center.x - half, center.y - half, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(center.x + half, center.y - half, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(center.x + half, center.y + half, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(center.x - half, center.y + half, 0);
            GL.End();
        }
    }
}
