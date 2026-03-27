using UnityEngine;
using System.Collections.Generic;
using Board.Input;

namespace BoardSketch
{
    public class SketchManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Renderer _canvasRenderer;
        [SerializeField] private Material _brushMaterial; // unused now but kept for serialization compat

        [Header("Brush Settings")]
        [SerializeField] private float _defaultBrushSize = 8f;
        [SerializeField] private int _maxUndoSteps = 20;

        [Header("Piece Tools")]
        [SerializeField] private PieceToolConfig _pieceToolConfig;

        [Header("Debug")]
        [SerializeField] private bool _drawTestPatternOnStart = false;
        [SerializeField] private bool _skipGalleryOnStart;

        private const int kWidth = 1920;
        private const int kHeight = 1080;

        private Texture2D _canvasTex;
        private Color32[] _pixels;
        private bool _pixelsDirty;
        private Material _canvasMat;
        private Camera _cam;
        private Dictionary<int, StrokeState> _activeStrokes = new Dictionary<int, StrokeState>();
        private UndoStack _undoStack;
        private ToolState _currentTool = new ToolState();
        private bool _isDirty;

        public bool IsDirty => _isDirty;
        public RenderTexture DrawRT => null; // Legacy compat

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _cam = Camera.main;
            InitCanvas();
            _undoStack = new UndoStack(_maxUndoSteps);
            if (_drawTestPatternOnStart)
                DrawStartupTestPattern();
        }

        private void InitCanvas()
        {
            _canvasTex = new Texture2D(kWidth, kHeight, TextureFormat.RGBA32, false);
            _canvasTex.filterMode = FilterMode.Bilinear;
            _pixels = _canvasTex.GetPixels32();

            // Clear to white
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = white;
            _canvasTex.SetPixels32(_pixels);
            _canvasTex.Apply();

            // Use the material already assigned to the Quad in the scene.
            // This avoids Shader.Find which doesn't work reliably in builds.
            if (_canvasRenderer != null)
            {
                _canvasMat = _canvasRenderer.material;
                _canvasMat.mainTexture = _canvasTex;
            }
        }

        private void DrawStartupTestPattern()
        {
            // Red diagonal to verify rendering works
            var red = new Color32(255, 50, 50, 255);
            for (int i = 0; i < 40; i++)
            {
                float t = i / 39f;
                Vector2 pos = new Vector2(t * kWidth, t * kHeight);
                BrushRenderer.Stamp(_pixels, kWidth, kHeight, pos, 20f, red);
            }
            // Blue circle in center
            var blue = new Color32(50, 100, 230, 255);
            for (int a = 0; a < 60; a++)
            {
                float rad = a / 60f * Mathf.PI * 2f;
                Vector2 pos = new Vector2(kWidth / 2f + Mathf.Cos(rad) * 200f, kHeight / 2f + Mathf.Sin(rad) * 200f);
                BrushRenderer.Stamp(_pixels, kWidth, kHeight, pos, 12f, blue);
            }
            _canvasTex.SetPixels32(_pixels);
            _canvasTex.Apply();
            Debug.Log("[BoardSketch] Test pattern drawn");
        }

        private void Update()
        {
            HandleFingerInput();
            HandleGlyphInput();

            // Upload full texture when dirty. With optimized CPU stamping,
            // this is simpler and avoids dirty-rect edge cases.
            if (_pixelsDirty)
            {
                _canvasTex.SetPixels32(_pixels);
                _canvasTex.Apply();
                _pixelsDirty = false;
            }
        }

        private void HandleFingerInput()
        {
            var contacts = BoardInput.GetActiveContacts(BoardContactType.Finger);

            foreach (var contact in contacts)
            {
                switch (contact.phase)
                {
                    case BoardContactPhase.Began:
                        OnStrokeBegan(contact);
                        break;
                    case BoardContactPhase.Moved:
                        OnStrokeMoved(contact);
                        break;
                    case BoardContactPhase.Stationary:
                        break;
                    case BoardContactPhase.Ended:
                    case BoardContactPhase.Canceled:
                        _activeStrokes.Remove(contact.contactId);
                        break;
                }
            }
        }

        private void OnStrokeBegan(BoardContact contact)
        {
            _undoStack.PushSnapshot(_canvasTex);

            Vector2 pos = BrushRenderer.ScreenToCanvas(contact.screenPosition, _cam, kWidth, kHeight);
            Color32 color = _currentTool.EffectiveColor32;

            var stroke = new StrokeState(contact.contactId, pos, _currentTool.EffectiveColor, _currentTool.brushSize, _currentTool.isEraser);
            _activeStrokes[contact.contactId] = stroke;

            BrushRenderer.Stamp(_pixels, kWidth, kHeight, pos, _currentTool.brushSize, color);
            _pixelsDirty = true;
            _isDirty = true;
        }

        private void OnStrokeMoved(BoardContact contact)
        {
            if (!_activeStrokes.TryGetValue(contact.contactId, out var stroke))
                return;

            Vector2 pos = BrushRenderer.ScreenToCanvas(contact.screenPosition, _cam, kWidth, kHeight);
            Color32 color = stroke.isEraser ? new Color32(255, 255, 255, 255) : (Color32)stroke.color;

            if (stroke.pointCount >= 3)
            {
                Vector2 p3Est = pos + (pos - stroke.prevPosition);
                BrushRenderer.StampSpline(_pixels, kWidth, kHeight,
                    stroke.prevPrevPosition, stroke.prevPosition, pos, p3Est,
                    stroke.brushSize, color, ref stroke.lastStampPosition);
            }
            else
            {
                BrushRenderer.StampLine(_pixels, kWidth, kHeight,
                    stroke.prevPosition, pos, stroke.brushSize, color,
                    ref stroke.lastStampPosition);
            }

            stroke.AddPoint(pos);
            _pixelsDirty = true;
        }

        private void HandleGlyphInput()
        {
            var glyphs = BoardInput.GetActiveContacts(BoardContactType.Glyph);
            foreach (var glyph in glyphs)
            {
                if (glyph.phase.IsEndedOrCanceled())
                    continue;

                if (_pieceToolConfig == null)
                {
                    if (glyph.phase == BoardContactPhase.Began)
                        Debug.Log("[BoardSketch] Glyph discovered: id=" + glyph.glyphId);
                    continue;
                }

                var dial = _pieceToolConfig.GetDial(glyph.glyphId);
                if (dial == null) continue;

                switch (dial.dialType)
                {
                    case PieceDialType.ColorWheel:
                        SetColor(PieceToolConfig.OrientationToColor(glyph.orientation));
                        OnToolChangedByPiece?.Invoke();
                        break;
                    case PieceDialType.BrushSize:
                        SetBrushSize(PieceToolConfig.OrientationToSize(glyph.orientation, dial.minValue, dial.maxValue));
                        OnToolChangedByPiece?.Invoke();
                        break;
                }
            }
        }

        public event System.Action OnToolChangedByPiece;
        public Color CurrentColor => _currentTool.color;
        public float CurrentBrushSize => _currentTool.brushSize;

        // --- Public API ---

        public void SetColor(Color color)
        {
            _currentTool.color = color;
            _currentTool.isEraser = false;
        }

        public void SetBrushSize(float size)
        {
            _currentTool.brushSize = size;
        }

        public void SetEraser()
        {
            _currentTool.isEraser = true;
        }

        public void Undo()
        {
            if (_undoStack.CanUndo)
            {
                _undoStack.PopAndApply(_canvasTex);
                _pixels = _canvasTex.GetPixels32();
            }
        }

        public void ClearCanvas()
        {
            _undoStack.PushSnapshot(_canvasTex);
            var white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = white;
            _canvasTex.SetPixels32(_pixels);
            _canvasTex.Apply();
            _isDirty = true;
        }

        public void LoadFromPNG(byte[] pngData)
        {
            _canvasTex.LoadImage(pngData);
            _pixels = _canvasTex.GetPixels32();
            _undoStack.Clear();
            _isDirty = false;
        }

        public Color32[] GetPixels() => _pixels;

        public void ApplyPixels()
        {
            _canvasTex.SetPixels32(_pixels);
            _canvasTex.Apply();
            _isDirty = true;
        }

        public byte[] ExportToPNG()
        {
            return _canvasTex.EncodeToPNG();
        }

        public void MarkClean()
        {
            _isDirty = false;
        }

        private void OnDestroy()
        {
            if (_canvasTex != null) Destroy(_canvasTex);
            if (_canvasMat != null) Destroy(_canvasMat);
        }
    }
}
