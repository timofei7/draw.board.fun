using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace BoardSketch
{
    [Serializable]
    public class SketchMetadata
    {
        public string id;
        public string description;
        public long createdAt;
        public long updatedAt;
        public string thumbnailPath;
    }

    public static class SketchStorage
    {
        private const string kIndexFile = "sketches.json";
        private const int kThumbWidth = 432;
        private const int kThumbHeight = 243;

        [Serializable]
        private class SketchIndex
        {
            public List<SketchMetadata> sketches = new List<SketchMetadata>();
        }

        private static string BasePath => Path.Combine(Application.persistentDataPath, "BoardSketch");

        public static List<SketchMetadata> ListSketches()
        {
            var index = LoadIndex();
            return index.sketches;
        }

        public static string SaveSketch(byte[] pngData, string existingId = null, string description = null)
        {
            EnsureDirectory();
            var index = LoadIndex();

            SketchMetadata meta;
            if (existingId != null)
            {
                meta = index.sketches.Find(s => s.id == existingId);
                if (meta == null)
                    meta = CreateNewMeta(index, description);
                else
                    meta.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            else
            {
                meta = CreateNewMeta(index, description);
            }

            if (description != null)
                meta.description = description;

            // Save full PNG
            string pngPath = Path.Combine(BasePath, meta.id + ".png");
            File.WriteAllBytes(pngPath, pngData);

            // Save thumbnail
            SaveThumbnail(pngData, meta);

            SaveIndex(index);
            return meta.id;
        }

        public static byte[] LoadSketch(string id)
        {
            string pngPath = Path.Combine(BasePath, id + ".png");
            if (!File.Exists(pngPath)) return null;
            return File.ReadAllBytes(pngPath);
        }

        public static Texture2D LoadThumbnail(string id)
        {
            string thumbPath = Path.Combine(BasePath, id + "_thumb.png");
            if (!File.Exists(thumbPath)) return null;

            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(thumbPath));
            return tex;
        }

        public static void DeleteSketch(string id)
        {
            var index = LoadIndex();
            index.sketches.RemoveAll(s => s.id == id);
            SaveIndex(index);

            string pngPath = Path.Combine(BasePath, id + ".png");
            string thumbPath = Path.Combine(BasePath, id + "_thumb.png");
            if (File.Exists(pngPath)) File.Delete(pngPath);
            if (File.Exists(thumbPath)) File.Delete(thumbPath);
        }

        private static SketchMetadata CreateNewMeta(SketchIndex index, string description)
        {
            var meta = new SketchMetadata
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 12),
                description = description ?? "Sketch " + DateTime.Now.ToString("MMM d, h:mm tt"),
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            index.sketches.Insert(0, meta);
            return meta;
        }

        private static void SaveThumbnail(byte[] pngData, SketchMetadata meta)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(pngData);

            var rt = RenderTexture.GetTemporary(kThumbWidth, kThumbHeight);
            Graphics.Blit(src, rt);

            var thumb = new Texture2D(kThumbWidth, kThumbHeight, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            thumb.ReadPixels(new Rect(0, 0, kThumbWidth, kThumbHeight), 0, 0);
            thumb.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            string thumbPath = Path.Combine(BasePath, meta.id + "_thumb.png");
            File.WriteAllBytes(thumbPath, thumb.EncodeToPNG());
            meta.thumbnailPath = thumbPath;

            UnityEngine.Object.Destroy(src);
            UnityEngine.Object.Destroy(thumb);
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(BasePath))
                Directory.CreateDirectory(BasePath);
        }

        private static SketchIndex LoadIndex()
        {
            EnsureDirectory();
            string path = Path.Combine(BasePath, kIndexFile);
            if (!File.Exists(path))
                return new SketchIndex();
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SketchIndex>(json) ?? new SketchIndex();
        }

        private static void SaveIndex(SketchIndex index)
        {
            string path = Path.Combine(BasePath, kIndexFile);
            File.WriteAllText(path, JsonUtility.ToJson(index, true));
        }
    }
}
