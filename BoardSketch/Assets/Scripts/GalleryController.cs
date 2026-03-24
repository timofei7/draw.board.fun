using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BoardSketch
{
    public class GalleryController : MonoBehaviour
    {
        [SerializeField] private AppStateManager _appState;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private Button _newSketchBtn;

        private List<GameObject> _thumbnailInstances = new List<GameObject>();

        private void Start()
        {
            if (_newSketchBtn)
                _newSketchBtn.onClick.AddListener(() => _appState.NewSketch());
            Refresh();
        }

        public void Refresh()
        {
            // Clear existing thumbnails
            foreach (var go in _thumbnailInstances)
                Destroy(go);
            _thumbnailInstances.Clear();

            // Load sketches and create thumbnails
            var sketches = SketchStorage.ListSketches();
            foreach (var meta in sketches)
            {
                var thumb = CreateThumbnail(meta);
                _thumbnailInstances.Add(thumb);
            }
        }

        private GameObject CreateThumbnail(SketchMetadata meta)
        {
            var go = new GameObject(meta.id, typeof(RectTransform));
            go.transform.SetParent(_gridContainer, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 180);

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.95f, 0.95f);

            // Button
            var btn = go.AddComponent<Button>();
            string capturedId = meta.id;
            btn.onClick.AddListener(() => _appState.OpenSketch(capturedId));

            var colors = btn.colors;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            btn.colors = colors;

            // Outline
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.7f, 0.7f, 0.7f);
            outline.effectDistance = new Vector2(1, -1);

            // Thumbnail image
            var tex = SketchStorage.LoadThumbnail(meta.id);
            if (tex != null)
            {
                var imgGO = new GameObject("Thumb", typeof(RectTransform));
                imgGO.transform.SetParent(go.transform, false);
                var imgRect = imgGO.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0.05f, 0.2f);
                imgRect.anchorMax = new Vector2(0.95f, 0.95f);
                imgRect.sizeDelta = Vector2.zero;
                imgRect.offsetMin = Vector2.zero;
                imgRect.offsetMax = Vector2.zero;
                var img = imgGO.AddComponent<RawImage>();
                img.texture = tex;
            }

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.2f);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.text = meta.description;
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.3f, 0.3f, 0.3f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return go;
        }
    }
}
