using UnityEngine;

namespace BoardSketch
{
    public class AppStateManager : MonoBehaviour
    {
        [SerializeField] private SketchManager _sketchManager;
        [SerializeField] private GameObject _galleryView;
        [SerializeField] private GameObject _canvasView;

        public string CurrentSketchId { get; set; }

        private void Start()
        {
            ShowGallery();
        }

        public void ShowGallery()
        {
            if (_galleryView) _galleryView.SetActive(true);
            if (_canvasView) _canvasView.SetActive(false);
        }

        public void NewSketch()
        {
            CurrentSketchId = null;
            _sketchManager.ClearCanvas();
            _sketchManager.MarkClean();
            ShowCanvas();
        }

        public void OpenSketch(string id)
        {
            byte[] png = SketchStorage.LoadSketch(id);
            if (png != null)
            {
                CurrentSketchId = id;
                _sketchManager.LoadFromPNG(png);
                ShowCanvas();
            }
        }

        public void BackToGallery()
        {
            // Auto-save if dirty
            if (_sketchManager.IsDirty)
            {
                byte[] png = _sketchManager.ExportToPNG();
                CurrentSketchId = SketchStorage.SaveSketch(png, CurrentSketchId);
                _sketchManager.MarkClean();
            }

            ShowGallery();
            // Refresh gallery thumbnails
            var gallery = FindAnyObjectByType<GalleryController>();
            if (gallery) gallery.Refresh();
        }

        private void ShowCanvas()
        {
            if (_galleryView) _galleryView.SetActive(false);
            if (_canvasView) _canvasView.SetActive(true);
        }
    }
}
