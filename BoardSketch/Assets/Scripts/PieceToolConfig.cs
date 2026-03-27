using UnityEngine;

namespace BoardSketch
{
    public enum PieceDialType
    {
        ColorWheel,
        BrushSize,
        Eraser
    }

    [CreateAssetMenu(fileName = "PieceToolConfig", menuName = "BoardSketch/Piece Tool Config")]
    public class PieceToolConfig : ScriptableObject
    {
        [System.Serializable]
        public class PieceDial
        {
            public int glyphId;
            public string label;
            public PieceDialType dialType;
            [Tooltip("For BrushSize: min px")]
            public float minValue = 2f;
            [Tooltip("For BrushSize: max px")]
            public float maxValue = 40f;
        }

        public PieceDial[] dials;

        public PieceDial GetDial(int glyphId)
        {
            if (dials == null) return null;
            for (int i = 0; i < dials.Length; i++)
                if (dials[i].glyphId == glyphId) return dials[i];
            return null;
        }

        public static Color OrientationToColor(float radians)
        {
            float hue = Mathf.Repeat(radians / (2f * Mathf.PI), 1f);
            return Color.HSVToRGB(hue, 0.8f, 0.9f);
        }

        public static float OrientationToSize(float radians, float min, float max)
        {
            float t = Mathf.Repeat(radians / (2f * Mathf.PI), 1f);
            return Mathf.Lerp(min, max, t);
        }
    }
}
