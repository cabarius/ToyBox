using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using UniRx;
using ModKit;
#if RT
using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
#endif

namespace ToyBox.classes.MainUI.Inventory {
    internal class SelectedCharacterObserver : IGlobalSubscriber,
                                                        ISubscriber {
        public static SelectedCharacterObserver Shared { get; private set; } = new();
        private IDisposable m_SelectedUnitUpdate;
        public UnitEntityData SelectedUnit = null;
        public delegate void NotifyDelegate();
        public NotifyDelegate Notifiers;

        public SelectedCharacterObserver() {
            EventBus.Subscribe((object)this);
            m_SelectedUnitUpdate = Game.Instance.SelectionCharacter.SelectedUnit.Subscribe(delegate(UnitReference u) {
                SelectedUnit = u.Value;
                Mod.Debug($"SelectedCharacterObserver - selected character changed to {SelectedUnit?.CharacterName.orange() ?? "null"} notifierCount: {Notifiers?.GetInvocationList()?.Length}");
                Notifiers?.Invoke();
            });
        }
    }
}
