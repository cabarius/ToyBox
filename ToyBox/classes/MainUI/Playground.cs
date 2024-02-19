using Kingmaker.Blueprints;
using ModKit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModKit {
    public static partial class ui {
        public static class Size {
            public const float Automatic = -1;
            public const float Expands = float.PositiveInfinity;

        }
        public class View : IDisposable {
            public View? background { get; set; } = null;
            public Color color { get; set; } = Color.clear;
            public float height { get; set; } = Size.Automatic;
            public float width { get; set; } = Size.Automatic;
            public RectOffset padding { get; set; } = new RectOffset();
            public void Dispose() => throw new NotImplementedException();
            public static explicit operator View(Color c) => new() { color = c };
        }
        public class Stack : View {
            public float spacing { get; set; } = 25;
        }

        public class HStack : Stack {

        }
        public class VStack : Stack {

        }
        public class List<ItemType> : Stack {
            public IEnumerable<ItemType>? items;
            public void ForEach(Action<ItemType> action) { }
            public void ForEach(Action<ItemType, int> action) { }
        }

        public class Label : View {
            public string text { get; set; } = "";
            public Label(string t) { this.text = t; }
        }
    }
}

namespace ToyBox {
    // A place to play...
    public static class Playground {
        public static void OnGUI() {
        }
    }
}


//                using (var stack = new UI.HStack { spacing = 25, color = RGBA.darkgrey.color() }) {
