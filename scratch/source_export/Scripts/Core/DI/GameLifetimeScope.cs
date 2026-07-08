using VContainer;
using VContainer.Unity;
using GameClient.Managers;
using GameClient.Network;

namespace GameClient.Core.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        public static IObjectResolver GlobalResolver { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(NetworkManager.Instance).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(LocalizationManager.Instance).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(UIManager.Instance).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(EventManager.Instance).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponent(TFSO.Managers.SettingsManager.Instance).AsImplementedInterfaces().AsSelf();

            builder.Register<GameClient.UI.Presenters.EntryPresenter>(Lifetime.Transient);
            builder.Register<GameClient.UI.Presenters.NoticePresenter>(Lifetime.Transient);
            builder.Register<GameClient.UI.Presenters.ZoneSelectPresenter>(Lifetime.Transient);
            builder.Register<GameClient.UI.Presenters.LoginPresenter>(Lifetime.Transient);
            builder.Register<GameClient.UI.Presenters.RegisterPresenter>(Lifetime.Transient);
            builder.Register<GameClient.UI.Presenters.SettingsPresenter>(Lifetime.Transient);
        }

        protected override void Awake()
        {
            base.Awake();
            if (Container != null)
            {
                GlobalResolver = Container;
            }
        }

        private void Start()
        {
            GlobalResolver = Container;
        }
    }
}
