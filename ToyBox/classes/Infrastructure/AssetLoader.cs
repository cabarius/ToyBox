using System.IO;
using TMPro;
using UnityEngine;
using ModKit;

namespace ToyBox {
    class AssetLoader {
        public static Sprite LoadInternal(string folder, string file, Vector2Int size) {
            return Image2Sprite.Create($"{Mod.modEntry.Path}Assets{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}{file}", size);
        }
        // Loosely based on https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        public static class Image2Sprite {
            public static string icons_folder = "";
            public static Sprite Create(string filePath, Vector2Int size) {
                var bytes = File.ReadAllBytes(icons_folder + filePath);
                var texture = new Texture2D(size.x, size.y, TextureFormat.ARGB32, false);
                _ = texture.LoadImage(bytes);
                return Sprite.Create(texture, new Rect(0, 0, size.x, size.y), new Vector2(0, 0));
            }
        }
    }

    public struct ModIcon {
        private readonly string name;
        private readonly Vector2Int size;

        public ModIcon(string name, int w, int h) {
            _sprite = null;
            this.name = name;
            this.size = new Vector2Int(w, h);
        }
        public ModIcon(string name, Vector2Int size) {
            _sprite = null;
            this.name = name;
            this.size = size;
        }

        private Sprite? _sprite;
        public Sprite Sprite => _sprite ??= (AssetLoader.LoadInternal("icons", name + ".png", size) ?? AssetLoader.LoadInternal("icons", "missing", new Vector2Int(32, 32)));

    }
}
