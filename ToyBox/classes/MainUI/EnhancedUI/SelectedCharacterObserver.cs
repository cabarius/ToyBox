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
using Kingmaker.EntitySystem.Interfaces;
#endif

namespace ToyBox.classes.MainUI.Inventory {
#if Wrath    
    internal class SelectedCharacterObserver : IGlobalSubscriber, ISubscriber {
#elif RT
    internal class SelectedCharacterObserver : IEntitySubscriber {
#endif

    public static SelectedCharacterObserver Shared { get; private set; } = new();
        private IDisposable m_SelectedUnitUpdate;
        public UnitEntityData SelectedUnit = null;
        public delegate void NotifyDelegate();
        public NotifyDelegate Notifiers;

        public SelectedCharacterObserver() {
            EventBus.Subscribe((object)this);
#if Wrath
            m_SelectedUnitUpdate = Game.Instance.SelectionCharacter.SelectedUnit.Subscribe(delegate(UnitReference u) {
#elif RT
            Game.Instance.SelectionCharacter.SelectedUnit.Subscribe<BaseUnitEntity>(delegate(BaseUnitEntity u) {
#endif
                SelectedUnit = u;
                Mod.Debug($"SelectedCharacterObserver - selected character changed to {SelectedUnit?.CharacterName.orange() ?? "null"} notifierCount: {Notifiers?.GetInvocationList()?.Length}");
                Notifiers?.Invoke();
            });
        }
#if RT
        public IEntity GetSubscribingEntity() => throw new NotImplementedException();
#endif
    }
}
