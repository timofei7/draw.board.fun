using UnityEngine;
using UnityEngine.UI;

namespace BoardSketch
{
    public class PieceIndicatorUI : MonoBehaviour
    {
        private RawImage _ringImage;
        private RawImage _indicatorDot;
        private PieceDialType _dialType;
        private RectTransform _rect;
        private RectTransform _dotRect;

        private const int kRingSize = 256;
        private const float kRingRadius = 100f;
        private const float kDotSize = 20f;

        private static Texture2D _colorWheelTex;
        private static Texture2D _sizeRingTex;
        private static Texture2D _eraserRingTex;

        public static PieceIndicatorUI Create(Transform uiParent, PieceDialType dialType, Vector2 screenPos)
        {
            var go = new GameObject("PieceIndicator_" + dialType, typeof(RectTransform));
            go.transform.SetParent(uiParent, false);

            var indicator = go.AddComponent<PieceIndicatorUI>();
            indicator._dialType = dialType;
            indicator._rect = go.GetComponent<RectTransform>();
            indicator._rect.sizeDelta = new Vector2(kRingSize, kRingSize);

            // Ring image
            var ringGO = new GameObject("Ring", typeof(RectTransform));
            ringGO.transform.SetParent(go.transform, false);
            var ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.sizeDelta = Vector2.zero;
            indicator._ringImage = ringGO.AddComponent<RawImage>();
            indicator._ringImage.texture = GetRingTexture(dialType);
            indicator._ringImage.raycastTarget = false;

            // Indicator dot (for color wheel and size dial)
            if (dialType != PieceDialType.Eraser)
            {
                var dotGO = new GameObject("Dot", typeof(RectTransform));
                dotGO.transform.SetParent(go.transform, false);
                indicator._dotRect = dotGO.GetComponent<RectTransform>();
                indicator._dotRect.sizeDelta = new Vector2(kDotSize, kDotSize);
                indicator._indicatorDot = dotGO.AddComponent<RawImage>();
                indicator._indicatorDot.color = Color.white;
                indicator._indicatorDot.raycastTarget = false;

                var outline = dotGO.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(2, -2);
            }

            indicator.UpdatePosition(screenPos);
            return indicator;
        }

        public void UpdatePosition(Vector2 screenPos)
        {
            // Position the indicator at the piece's screen location
            _rect.position = new Vector3(screenPos.x, screenPos.y, 0);
        }

        public void UpdateValue(float orientation)
        {
            float angle = orientation * Mathf.Rad2Deg;

            if (_dialType == PieceDialType.ColorWheel)
            {
                // Move dot around the ring at the hue angle
                float rad = orientation;
                float dotDist = kRingSize * 0.38f;
                if (_dotRect != null)
                {
                    _dotRect.anchoredPosition = new Vector2(
                        Mathf.Cos(rad) * dotDist,
                        Mathf.Sin(rad) * dotDist
                    );
                    // Color the dot to match selected hue
                    float hue = Mathf.Repeat(orientation / (2f * Mathf.PI), 1f);
                    _indicatorDot.color = Color.HSVToRGB(hue, 1f, 1f);
                }
            }
            else if (_dialType == PieceDialType.BrushSize)
            {
                float rad = orientation;
                float dotDist = kRingSize * 0.38f;
                if (_dotRect != null)
                {
                    _dotRect.anchoredPosition = new Vector2(
                        Mathf.Cos(rad) * dotDist,
                        Mathf.Sin(rad) * dotDist
                    );
                    // Scale dot to represent current brush size
                    float t = Mathf.Repeat(orientation / (2f * Mathf.PI), 1f);
                    float scale = Mathf.Lerp(0.5f, 2f, t);
                    _dotRect.localScale = Vector3.one * scale;
                }
            }
        }

        private static Texture2D GetRingTexture(PieceDialType type)
        {
            switch (type)
            {
                case PieceDialType.ColorWheel: return GetOrCreateColorWheel();
                case PieceDialType.BrushSize: return GetOrCreateSizeRing();
                case PieceDialType.Eraser: return GetOrCreateEraserRing();
                default: return GetOrCreateColorWheel();
            }
        }

        private static Texture2D GetOrCreateColorWheel()
        {
            if (_colorWheelTex != null) return _colorWheelTex;

            _colorWheelTex = new Texture2D(kRingSize, kRingSize, TextureFormat.RGBA32, false);
            var pixels = new Color[kRingSize * kRingSize];
            float center = kRingSize / 2f;
            float outerR = kRingSize / 2f - 2f;
            float innerR = outerR - 28f;

            for (int y = 0; y < kRingSize; y++)
            {
                for (int x = 0; x < kRingSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= innerR && dist <= outerR)
                    {
                        float angle = Mathf.Atan2(dy, dx);
                        float hue = Mathf.Repeat(angle / (2f * Mathf.PI), 1f);
                        float edgeFade = Mathf.Clamp01(Mathf.Min(dist - innerR, outerR - dist) / 3f);
                        Color c = Color.HSVToRGB(hue, 0.9f, 1f);
                        c.a = edgeFade;
                        pixels[y * kRingSize + x] = c;
                    }
                    else
                    {
                        pixels[y * kRingSize + x] = Color.clear;
                    }
                }
            }

            _colorWheelTex.SetPixels(pixels);
            _colorWheelTex.Apply();
            return _colorWheelTex;
        }

        private static Texture2D GetOrCreateSizeRing()
        {
            if (_sizeRingTex != null) return _sizeRingTex;

            _sizeRingTex = new Texture2D(kRingSize, kRingSize, TextureFormat.RGBA32, false);
            var pixels = new Color[kRingSize * kRingSize];
            float center = kRingSize / 2f;
            float ringR = kRingSize / 2f - 15f;

            // Draw dots of increasing size around the ring
            for (int i = 0; i < 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2f;
                float t = i / 23f;
                float dotSize = Mathf.Lerp(2f, 12f, t);
                float px = center + Mathf.Cos(angle) * ringR;
                float py = center + Mathf.Sin(angle) * ringR;

                for (int y = 0; y < kRingSize; y++)
                {
                    for (int x = 0; x < kRingSize; x++)
                    {
                        float dx = x - px;
                        float dy = y - py;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist < dotSize)
                        {
                            float alpha = Mathf.Clamp01(1f - dist / dotSize);
                            Color existing = pixels[y * kRingSize + x];
                            Color dot = new Color(0.3f, 0.3f, 0.3f, alpha);
                            // Alpha blend
                            float a = dot.a;
                            pixels[y * kRingSize + x] = new Color(
                                dot.r * a + existing.r * (1 - a),
                                dot.g * a + existing.g * (1 - a),
                                dot.b * a + existing.b * (1 - a),
                                Mathf.Max(existing.a, a)
                            );
                        }
                    }
                }
            }

            _sizeRingTex.SetPixels(pixels);
            _sizeRingTex.Apply();
            return _sizeRingTex;
        }

        private static Texture2D GetOrCreateEraserRing()
        {
            if (_eraserRingTex != null) return _eraserRingTex;

            _eraserRingTex = new Texture2D(kRingSize, kRingSize, TextureFormat.RGBA32, false);
            var pixels = new Color[kRingSize * kRingSize];
            float center = kRingSize / 2f;
            float outerR = kRingSize / 2f - 2f;
            float innerR = outerR - 4f;

            for (int y = 0; y < kRingSize; y++)
            {
                for (int x = 0; x < kRingSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= innerR && dist <= outerR)
                    {
                        float edgeFade = Mathf.Clamp01(Mathf.Min(dist - innerR, outerR - dist) / 2f);
                        pixels[y * kRingSize + x] = new Color(0.6f, 0.6f, 0.6f, edgeFade * 0.7f);
                    }
                    else
                    {
                        pixels[y * kRingSize + x] = Color.clear;
                    }
                }
            }

            _eraserRingTex.SetPixels(pixels);
            _eraserRingTex.Apply();
            return _eraserRingTex;
        }
    }
}
