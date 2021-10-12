using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    public abstract class Mutator {
        public delegate void Perform(object source, object target = default, int count = 1, object value = default);

        public delegate bool CanPerform(object source, object target = default, object value = default);

        private static Dictionary<Type, List<Mutator>> actionsForType;

        public static List<Mutator> ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, List<Mutator>> { };
                BlueprintActions.InitializeActions();
            }

            actionsForType.TryGetValue(type, out var result);

            if (result == null) {
                var baseType = type.BaseType;

                if (baseType != null) {
                    result = ActionsForType(baseType);
                }

                result ??= new List<Mutator> { };

                actionsForType[type] = result;
            }

            return result;
        }

        public static IEnumerable<Mutator> ActionsForSource(object source) => ActionsForType(source.GetType());

        public static void Register<Source, Target>(string name, Mutator<Source, Target>.Perform perform, Mutator<Source, Target>.CanPerform canPerform = null, bool isRepeatable = false) {
            var action = new Mutator<Source, Target>(name, perform, canPerform, isRepeatable);
            var type = action.BlueprintType;
            actionsForType.TryGetValue(type, out var existing);
            existing ??= new List<Mutator> { };
            existing.Add(action);
            actionsForType[type] = existing;
        }

        public string name { get; protected set; }

        public Perform action;

        public CanPerform canPerform;

        protected Mutator(string name, bool isRepeatable) {
            this.name = name;
            this.isRepeatable = isRepeatable;
        }

        public bool isRepeatable;

        public abstract Type BlueprintType { get; }
    }

    public class Mutator<Source, Target> : Mutator {
        public new delegate void Perform(Source bp, Target target, int count = 1, object value = default);

        public new delegate bool CanPerform(Source bp, Target target, object value = default);

        public Mutator(string name, Perform action, CanPerform canPerform = null, bool isRepeatable = false) : base(name, isRepeatable) {
            this.action = (bp, target, n, value) => action((Source)bp, (Target)target, n, value);
            this.canPerform = (bp, target, value) => Main.IsInGame && bp is Source bpt && (canPerform?.Invoke(bpt, (Target)target, value) ?? true);
        }

        public override Type BlueprintType => typeof(Source);
    }
}