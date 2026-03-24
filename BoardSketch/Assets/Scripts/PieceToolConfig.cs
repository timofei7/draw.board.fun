using UnityEngine;

namespace BoardSketch
{
    [CreateAssetMenu(fileName = "PieceToolConfig", menuName = "BoardSketch/Piece Tool Config")]
    public class PieceToolConfig : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public int glyphId;
            public string toolName;
            public Color brushColor = Color.black;
            public float brushSize = 8f;
            public bool isEraser;
        }

        public Entry[] entries;

        public Entry GetTool(int glyphId)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].glyphId == glyphId) return entries[i];
            return null;
        }
    }
}
