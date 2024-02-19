using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    // This class encapsulates the ability to show GUI for displaying and or editing an object.  Not all editors will actually modify values but they will be included here to keep things simple
    public abstract class Editor {

        // the return value is intended to single the enclosing UI that something has changed 
        public delegate bool MainGUI(ref bool showDetail, object[] argv);
        public delegate bool DetailGUI(ref bool showDetail, object[] argv);
        public string name { get; protected set; }

        public MainGUI OnGUI;
        public DetailGUI OnDetail;
        protected Editor(string name) {
            this.name = name;
        }
        public abstract Type EditorType { get; }

        private static Dictionary<Type, List<Editor>> editorsForType;

        public static List<Editor> EditorsForType(Type type) {
            if (editorsForType == null) {
                editorsForType = new Dictionary<Type, List<Editor>>();
                BlueprintActions.InitializeActions();
            }
            editorsForType.TryGetValue(type, out var editors);
            if (editors == null) {
                var baseType = type.BaseType;
                if (baseType != null) {
                    editors = EditorsForType(baseType);
                }
                editors ??= new List<Editor> { };
                editorsForType[type] = editors;
            }
            return editors;
        }

        public static IEnumerable<object> EditorsFor(object obj) => EditorsForType(obj.GetType());
        public static IEnumerable<object> EditorsFor(object source, object target)
            => EditorsForType((source.GetType(), target.GetType()).GetType());

        public static void Register(Editor editor) {
            var type = editor.EditorType;
            editorsForType.TryGetValue(type, out var existing);
            existing ??= new List<Editor> { };
            existing.Add(editor);
            editorsForType[type] = existing;
        }
        public static void Register<T>(string name, Editor<T>.MainGUI main, Editor<T>.DetailGUI detail)
            => Register((Editor)new Editor<T>(name, main, detail));
        public static void Register<Source, Target>(string name, Editor<Source, Target>.MainGUI main, Editor<Source, Target>.DetailGUI detail) => Register(new Editor<Source, Target>(name, main, detail));
    }
    public class Editor<T> : Editor {
        public new delegate bool MainGUI(ref bool showDetail, T arg, object[] argv);
        public new delegate bool DetailGUI(ref bool showDetail, T arg, object[] argv);
        public override Type EditorType => typeof(T);
        public Editor(string name, MainGUI main, DetailGUI detail) : base(name) {
            OnGUI = (ref bool showDetail, object[] argv) => main(ref showDetail, (T)argv[0], argv.Skip(1).ToArray());
            OnDetail = (ref bool showDetail, object[] argv) => detail(ref showDetail, (T)argv[0], argv.Skip(1).ToArray());
        }
    }

    public class Editor<Source, Target> : Editor {
        public new delegate bool MainGUI(ref bool showDetail, Source source, Target target, object[] argv);
        public new delegate bool DetailGUI(ref bool showDetail, Source source, Target target, object[] argv);
        public override Type EditorType => typeof((Source, Target));
        public Editor(string name, MainGUI main, DetailGUI detail) : base(name) {
            OnGUI = (ref bool showDetail, object[] argv) => main(ref showDetail, (Source)argv[0], (Target)argv[1], argv.Skip(2).ToArray());
            OnDetail = (ref bool showDetail, object[] argv) => detail(ref showDetail, (Source)argv[0], (Target)argv[1], argv.Skip(2).ToArray());
        }
    }
}