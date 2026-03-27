using UnityEngine;

namespace BoardSketch
{
    [System.Serializable]
    public class ToolState
    {
        public Color color = Color.black;
        public float brushSize = 8f;
        public bool isEraser;

        public ToolState() { }

        public ToolState(Color color, float brushSize, bool isEraser = false)
        {
            this.color = color;
            this.brushSize = brushSize;
            this.isEraser = isEraser;
        }

        public Color EffectiveColor => isEraser ? Color.white : color;
        public Color32 EffectiveColor32 => isEraser ? new Color32(255, 255, 255, 255) : (Color32)color;
    }
}
