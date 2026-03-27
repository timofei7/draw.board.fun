using UnityEngine;
using System.Collections.Generic;

namespace BoardSketch
{
    public static class BrushRenderer
    {
        // Cache pre-baked brush alpha masks by diameter
        private static Dictionary<int, byte[]> _brushCache = new Dictionary<int, byte[]>();

        private static byte[] GetBrushAlpha(int diameter)
        {
            if (_brushCache.TryGetValue(diameter, out var cached))
                return cached;

            byte[] alpha = new byte[diameter * diameter];
            float center = diameter / 2f;
            float radius = diameter / 2f;
            float radiusSq = radius * radius;

            for (int y = 0; y < diameter; y++)
            {
                float dy = y - center + 0.5f;
                float dySq = dy * dy;
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center + 0.5f;
                    float distSq = dx * dx + dySq;
                    if (distSq >= radiusSq)
                    {
                        alpha[y * diameter + x] = 0;
                        continue;
                    }
                    float dist = Mathf.Sqrt(distSq) / radius;
                    // SmoothStep falloff
                    float t = dist * dist * (3f - 2f * dist);
                    alpha[y * diameter + x] = (byte)((1f - t) * 255f);
                }
            }

            _brushCache[diameter] = alpha;
            return alpha;
        }

        /// <summary>
        /// Convert screen position to canvas pixel coordinates (Y-up, matching Texture2D convention).
        /// </summary>
        public static Vector2 ScreenToCanvas(Vector2 screenPosition, Camera cam, int canvasW, int canvasH)
        {
#if UNITY_EDITOR
            // Editor: use camera projection to handle arbitrary Game View aspect
            float screenY = Screen.height - screenPosition.y;
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenY, 11f));
            float rtX = (worldPos.x + 9.6f) / 19.2f * canvasW;
            float rtY = (worldPos.y + 5.4f) / 10.8f * canvasH;
            return new Vector2(rtX, rtY);
#else
            // Device: screen is 1920x1080. SDK gives Y-down, Texture2D is Y-up.
            return new Vector2(screenPosition.x, canvasH - screenPosition.y);
#endif
        }

        /// <summary>
        /// Fast brush stamp using pre-baked alpha mask and integer blending.
        /// </summary>
        public static void Stamp(Color32[] pixels, int width, int height, Vector2 center, float brushSize, Color32 color)
        {
            int diameter = Mathf.Max(Mathf.RoundToInt(brushSize), 2);
            byte[] brushAlpha = GetBrushAlpha(diameter);

            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);
            int halfD = diameter / 2;

            int brushStartX = 0, brushStartY = 0;
            int startX = cx - halfD;
            int startY = cy - halfD;
            int endX = startX + diameter;
            int endY = startY + diameter;

            // Clip to canvas bounds
            if (startX < 0) { brushStartX = -startX; startX = 0; }
            if (startY < 0) { brushStartY = -startY; startY = 0; }
            if (endX > width) endX = width;
            if (endY > height) endY = height;

            int cr = color.r, cg = color.g, cb = color.b;

            for (int py = startY; py < endY; py++)
            {
                int by = brushStartY + (py - startY);
                int canvasRow = py * width;
                int brushRow = by * diameter;

                for (int px = startX; px < endX; px++)
                {
                    int bx = brushStartX + (px - startX);
                    int a = brushAlpha[brushRow + bx];
                    if (a == 0) continue;

                    int idx = canvasRow + px;
                    Color32 dst = pixels[idx];

                    // Integer alpha blend: (src * a + dst * (255 - a)) / 256
                    int invA = 255 - a;
                    pixels[idx] = new Color32(
                        (byte)((cr * a + dst.r * invA) >> 8),
                        (byte)((cg * a + dst.g * invA) >> 8),
                        (byte)((cb * a + dst.b * invA) >> 8),
                        255);
                }
            }
        }

        /// <summary>
        /// Stamp along a line. Guarantees no gaps even during fast movement.
        /// </summary>
        public static void StampLine(Color32[] pixels, int width, int height,
            Vector2 from, Vector2 to, float brushSize, Color32 color,
            ref Vector2 lastStampPos)
        {
            float spacing = Mathf.Max(brushSize * 0.3f, 1.5f);
            Vector2 delta = to - lastStampPos;
            float dist = delta.magnitude;

            if (dist < 0.5f) return;

            // Always stamp at least at 'to' for responsiveness
            if (dist < spacing)
            {
                Stamp(pixels, width, height, to, brushSize, color);
                lastStampPos = to;
                return;
            }

            Vector2 dir = delta / dist;
            float traveled = 0f;
            while (traveled < dist)
            {
                Vector2 pos = lastStampPos + dir * traveled;
                Stamp(pixels, width, height, pos, brushSize, color);
                traveled += spacing;
            }
            // Always stamp the endpoint
            Stamp(pixels, width, height, to, brushSize, color);
            lastStampPos = to;
        }

        /// <summary>
        /// Stamp along a Catmull-Rom spline for smooth curves.
        /// </summary>
        public static void StampSpline(Color32[] pixels, int width, int height,
            Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
            float brushSize, Color32 color, ref Vector2 lastStampPos)
        {
            float spacing = Mathf.Max(brushSize * 0.3f, 1.5f);

            float chordLen = (p2 - p1).magnitude;
            float ctrlLen = (p1 - p0).magnitude + (p2 - p1).magnitude + (p3 - p2).magnitude;
            float estLen = Mathf.Max((chordLen + ctrlLen) / 2f, 1f);
            int steps = Mathf.Max(Mathf.CeilToInt(estLen / spacing), 2);
            float tStep = 1f / steps;

            for (int i = 0; i <= steps; i++)
            {
                float t = i * tStep;
                Vector2 pos = CatmullRom(p0, p1, p2, p3, t);

                if ((pos - lastStampPos).sqrMagnitude >= spacing * spacing * 0.8f)
                {
                    Stamp(pixels, width, height, pos, brushSize, color);
                    lastStampPos = pos;
                }
            }
            // Always stamp the endpoint
            Stamp(pixels, width, height, p2, brushSize, color);
            lastStampPos = p2;
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

        // Legacy compat
        public static Texture2D GetDefaultBrushTexture()
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return tex;
        }
    }
}
