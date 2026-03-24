using UnityEngine;

namespace BoardSketch
{
    public class UndoStack
    {
        private byte[][] _snapshots;
        private int _head;
        private int _count;
        private int _capacity;

        public bool CanUndo => _count > 0;

        public UndoStack(int capacity = 20)
        {
            _capacity = capacity;
            _snapshots = new byte[capacity][];
            _head = 0;
            _count = 0;
        }

        public void PushSnapshot(RenderTexture rt)
        {
            // All Texture2D operations must happen on the main thread.
            // PNG encoding causes a brief hitch (~20-50ms) but is acceptable for V1.
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            byte[] png = tex.EncodeToPNG();
            Object.Destroy(tex);

            _snapshots[_head] = png;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }

        public void PopAndApply(RenderTexture target)
        {
            if (_count == 0) return;

            _head = (_head - 1 + _capacity) % _capacity;
            _count--;

            byte[] png = _snapshots[_head];
            _snapshots[_head] = null;

            if (png == null) return;

            var tex = new Texture2D(2, 2);
            tex.LoadImage(png);
            Graphics.Blit(tex, target);
            Object.Destroy(tex);
        }

        public void Clear()
        {
            for (int i = 0; i < _capacity; i++)
                _snapshots[i] = null;
            _head = 0;
            _count = 0;
        }
    }
}
