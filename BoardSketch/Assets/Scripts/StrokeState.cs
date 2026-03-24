using UnityEngine;

namespace BoardSketch
{
    public class StrokeState
    {
        public int contactId;
        public Color color;
        public float brushSize;
        public bool isEraser;
        public Vector2 lastStampPosition;
        public Vector2 prevPrevPosition;
        public Vector2 prevPosition;
        public int pointCount;

        public StrokeState(int contactId, Vector2 startPosition, Color color, float brushSize, bool isEraser)
        {
            this.contactId = contactId;
            this.color = color;
            this.brushSize = brushSize;
            this.isEraser = isEraser;
            this.lastStampPosition = startPosition;
            this.prevPrevPosition = startPosition;
            this.prevPosition = startPosition;
            this.pointCount = 1;
        }

        public void AddPoint(Vector2 position)
        {
            prevPrevPosition = prevPosition;
            prevPosition = position;
            pointCount++;
        }
    }
}
