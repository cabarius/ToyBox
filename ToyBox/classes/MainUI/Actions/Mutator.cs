using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
#if false
    public abstract class Mutator {
        // the return value is intended to single the enclosing UI that something has changed 
        public delegate bool MainGUI(ref bool showDetail, object source, object target, object value = default, int count = 1);
        public delegate bool DetailGUI(ref bool showDetail, object source, object target, object value = default, int count = 1);

        // for now we will assume that the source type takes precedence. We may want to generalize this later to do the full lattice thing
        private static Dictionary<Type, Dictionary<Type, List<Mutator>>> actionsForTypes;

        public static List<Mutator> ActionsForTypes(Type source, Type target) {
            if (actionsForTypes == null) {
                actionsForTypes = new Dictionary<Type, Dictionary<Type, List<Mutator>>> { };
                MutatorActions.InitializeActions();
            }

            var actionsForTargetType = actionsForTypes.GetValueOrDefault(source, new Dictionary<Type, List<Mutator>> { });
            actionsForTargetType.TryGetValue(target, out var result);

            if (result == null) {
                var sourceBase = source.BaseType;

                if (sourceBase != null) {
                    result = ActionsForTypes(sourceBase, target);
                }

                result ??= new List<Mutator> { };

                actionsForType[type] = result;
            }

            return result;
        }

        public static IEnumerable<Mutator> ActionsForSource(object source) => ActionsForType(source.GetType());

        public static void Register<Source, Target>(string name, Mutator<Source, Target>.OnGUIAction action, bool isRepeatable = false) {
            var mutator = new Mutator<Source, Target>(name, action, isRepeatable);
            var sourceType = mutator.SourceType;
            var targetType = mutator.TargetType;
            actionsForType.TryGetValue(type, out var existing);
            existing ??= new List<Mutator> { };
            existing.Add(action);
            actionsForType[type] = existing;
        }

        public string name { get; protected set; }

        public MainGUI OnGUI;
        public DetailGUI OnDetailGUI;

        protected Mutator(string name, bool isRepeatable) {
            this.name = name;
            this.isRepeatable = isRepeatable;
        }

        public bool isRepeatable;

        public abstract Type SourceType { get; }
        public abstract Type TargetType { get; }
    }

    public class Mutator<Source, Target> : Mutator {
        public new delegate bool OnGUIAction(Source source, Target target, ref bool showDetail, object value = default, int count = 1);

        public Mutator(string name, OnGUIAction action, bool isRepeatable = false) : base(name, isRepeatable) =>
            this.OnGUI = (source, target, showDetail, value, count) => OnGUI((Source)source, (Target)target, value, count);

        public override Type BlueprintType => typeof(Source);
    }
#endif
}