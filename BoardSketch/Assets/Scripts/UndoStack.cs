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

        public void PushSnapshot(Texture2D tex)
        {
            byte[] png = tex.EncodeToPNG();
            _snapshots[_head] = png;
            _head = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }

        public void PopAndApply(Texture2D target)
        {
            if (_count == 0) return;

            _head = (_head - 1 + _capacity) % _capacity;
            _count--;

            byte[] png = _snapshots[_head];
            _snapshots[_head] = null;

            if (png != null)
                target.LoadImage(png);
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
