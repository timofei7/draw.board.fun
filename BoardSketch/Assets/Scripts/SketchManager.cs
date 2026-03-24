using UnityEngine;
using System.Collections.Generic;
using Board.Input;

namespace BoardSketch
{
    public class SketchManager : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Renderer _canvasRenderer;

        [Header("Brush Settings")]
        [SerializeField] private float _defaultBrushSize = 8f;
        [SerializeField] private int _maxUndoSteps = 20;

        [Header("Piece Tools")]
        [SerializeField] private PieceToolConfig _pieceToolConfig;

        private RenderTexture _drawRT;
        private Material _brushMat;
        private Material _canvasMat;
        private Camera _cam;
        private Dictionary<int, StrokeState> _activeStrokes = new Dictionary<int, StrokeState>();
        private UndoStack _undoStack;
        private ToolState _currentTool = new ToolState();
        private bool _isDirty;

        public bool IsDirty => _isDirty;
        public RenderTexture DrawRT => _drawRT;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _cam = Camera.main;
            InitCanvas();
            _undoStack = new UndoStack(_maxUndoSteps);
        }

        private void InitCanvas()
        {
            _drawRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            _drawRT.Create();
            ClearToWhite();

            _brushMat = new Material(Shader.Find("BoardSketch/BrushStamp"));

            _canvasMat = new Material(Shader.Find("Unlit/Texture"));
            _canvasMat.mainTexture = _drawRT;

            if (_canvasRenderer != null)
                _canvasRenderer.material = _canvasMat;
        }

        private void Update()
        {
            HandleFingerInput();
            HandleGlyphInput();
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
            _undoStack.PushSnapshot(_drawRT);

            // Convert SDK position (Y-down) to GL position (Y-up) for rendering
            Vector2 glPos = BrushRenderer.ScreenToRT(contact.screenPosition, _cam);

            Color color = _currentTool.EffectiveColor;
            var stroke = new StrokeState(
                contact.contactId,
                glPos,
                color,
                _currentTool.brushSize,
                _currentTool.isEraser
            );
            _activeStrokes[contact.contactId] = stroke;

            BrushRenderer.StampSingle(_drawRT, _brushMat, glPos, color, _currentTool.brushSize);
            _isDirty = true;
        }

        private void OnStrokeMoved(BoardContact contact)
        {
            if (!_activeStrokes.TryGetValue(contact.contactId, out var stroke))
                return;

            Vector2 glPos = BrushRenderer.ScreenToRT(contact.screenPosition, _cam);
            Color color = stroke.isEraser ? Color.white : stroke.color;

            if (stroke.pointCount >= 3)
            {
                Vector2 p3Estimate = glPos + (glPos - stroke.prevPosition);
                BrushRenderer.StampAlongPathSmooth(
                    _drawRT, _brushMat,
                    stroke.prevPrevPosition,
                    stroke.prevPosition,
                    glPos,
                    p3Estimate,
                    color, stroke.brushSize,
                    ref stroke.lastStampPosition
                );
            }
            else
            {
                BrushRenderer.StampAlongPath(
                    _drawRT, _brushMat,
                    stroke.prevPosition,
                    glPos,
                    color, stroke.brushSize,
                    ref stroke.lastStampPosition
                );
            }

            stroke.AddPoint(glPos);
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
                    // Discovery mode: log any detected glyph
                    if (glyph.phase == BoardContactPhase.Began)
                        Debug.Log("[BoardSketch] Glyph discovered: id=" + glyph.glyphId + " orientation=" + glyph.orientation);
                    continue;
                }

                var dial = _pieceToolConfig.GetDial(glyph.glyphId);
                if (dial == null)
                {
                    if (glyph.phase == BoardContactPhase.Began)
                        Debug.Log("[BoardSketch] Unknown glyph id=" + glyph.glyphId);
                    continue;
                }

                // Process dial rotation every frame
                switch (dial.dialType)
                {
                    case PieceDialType.ColorWheel:
                        var color = PieceToolConfig.OrientationToColor(glyph.orientation);
                        SetColor(color);
                        OnToolChangedByPiece?.Invoke();
                        break;

                    case PieceDialType.BrushSize:
                        float size = PieceToolConfig.OrientationToSize(glyph.orientation, dial.minValue, dial.maxValue);
                        SetBrushSize(size);
                        OnToolChangedByPiece?.Invoke();
                        break;
                }
            }
        }

        /// <summary>
        /// Fired when a piece dial changes the current tool, so the toolbar can update its indicators.
        /// </summary>
        public event System.Action OnToolChangedByPiece;

        public Color CurrentColor => _currentTool.color;
        public float CurrentBrushSize => _currentTool.brushSize;

        // --- Public API for toolbar ---

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
                _undoStack.PopAndApply(_drawRT);
        }

        public void ClearCanvas()
        {
            _undoStack.PushSnapshot(_drawRT);
            ClearToWhite();
            _isDirty = true;
        }

        public void LoadFromPNG(byte[] pngData)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(pngData);
            Graphics.Blit(tex, _drawRT);
            Object.Destroy(tex);
            _undoStack.Clear();
            _isDirty = false;
        }

        public byte[] ExportToPNG()
        {
            var tex = new Texture2D(_drawRT.width, _drawRT.height, TextureFormat.RGBA32, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _drawRT;
            tex.ReadPixels(new Rect(0, 0, _drawRT.width, _drawRT.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            byte[] png = tex.EncodeToPNG();
            Object.Destroy(tex);
            return png;
        }

        public void MarkClean()
        {
            _isDirty = false;
        }

        private void ClearToWhite()
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _drawRT;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = prev;
        }

        private void OnDestroy()
        {
            if (_drawRT != null)
            {
                _drawRT.Release();
                Object.Destroy(_drawRT);
            }
            if (_brushMat != null) Object.Destroy(_brushMat);
            if (_canvasMat != null) Object.Destroy(_canvasMat);
        }
    }
}
