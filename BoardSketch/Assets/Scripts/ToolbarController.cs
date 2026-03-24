using UnityEngine;
using UnityEngine.UI;

namespace BoardSketch
{
    public class ToolbarController : MonoBehaviour
    {
        [SerializeField] private SketchManager _sketchManager;

        [Header("Color Buttons")]
        [SerializeField] private Button _blackBtn;
        [SerializeField] private Button _redBtn;
        [SerializeField] private Button _blueBtn;
        [SerializeField] private Button _greenBtn;
        [SerializeField] private Button _yellowBtn;
        [SerializeField] private Button _eraserBtn;

        [Header("Size Buttons")]
        [SerializeField] private Button _smallBtn;
        [SerializeField] private Button _mediumBtn;
        [SerializeField] private Button _largeBtn;

        [Header("Action Buttons")]
        [SerializeField] private Button _undoBtn;
        [SerializeField] private Button _clearBtn;
        [SerializeField] private Button _saveBtn;

        [Header("Selection Indicator")]
        [SerializeField] private Image _colorIndicator;
        [SerializeField] private Image _sizeIndicator;

        private Color _currentColor = Color.black;
        private float _currentSize = 8f;

        private void Start()
        {
            if (_blackBtn) _blackBtn.onClick.AddListener(() => SelectColor(Color.black));
            if (_redBtn) _redBtn.onClick.AddListener(() => SelectColor(new Color(0.9f, 0.2f, 0.2f)));
            if (_blueBtn) _blueBtn.onClick.AddListener(() => SelectColor(new Color(0.2f, 0.4f, 0.9f)));
            if (_greenBtn) _greenBtn.onClick.AddListener(() => SelectColor(new Color(0.2f, 0.8f, 0.3f)));
            if (_yellowBtn) _yellowBtn.onClick.AddListener(() => SelectColor(new Color(0.95f, 0.8f, 0.1f)));
            if (_eraserBtn) _eraserBtn.onClick.AddListener(SelectEraser);

            if (_smallBtn) _smallBtn.onClick.AddListener(() => SelectSize(4f));
            if (_mediumBtn) _mediumBtn.onClick.AddListener(() => SelectSize(12f));
            if (_largeBtn) _largeBtn.onClick.AddListener(() => SelectSize(24f));

            if (_undoBtn) _undoBtn.onClick.AddListener(() => _sketchManager.Undo());
            if (_clearBtn) _clearBtn.onClick.AddListener(() => _sketchManager.ClearCanvas());
            if (_saveBtn) _saveBtn.onClick.AddListener(OnSave);

            SelectColor(Color.black);
            SelectSize(8f);
        }

        private void SelectColor(Color color)
        {
            _currentColor = color;
            _sketchManager.SetColor(color);
            if (_colorIndicator) _colorIndicator.color = color;
        }

        private void SelectEraser()
        {
            _sketchManager.SetEraser();
            if (_colorIndicator) _colorIndicator.color = Color.white;
        }

        private void SelectSize(float size)
        {
            _currentSize = size;
            _sketchManager.SetBrushSize(size);
            if (_sizeIndicator)
            {
                float scale = size / 24f;
                _sizeIndicator.rectTransform.localScale = Vector3.one * Mathf.Max(scale, 0.3f);
            }
        }

        private void OnSave()
        {
            byte[] png = _sketchManager.ExportToPNG();
            string path = System.IO.Path.Combine(Application.persistentDataPath, "sketch_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            System.IO.File.WriteAllBytes(path, png);
            _sketchManager.MarkClean();
            Debug.Log("[BoardSketch] Saved to " + path);
        }
    }
}
