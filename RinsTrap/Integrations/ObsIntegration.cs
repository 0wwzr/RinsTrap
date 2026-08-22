using OBS.WebSocket.NET;

namespace RinsTrap.Integrations
{
    public class ObsIntegration : IDisposable
    {
        private ObsWebSocket? _obs;
        private bool _connected;

        public bool IsEnabled => App.Settings.Prop.UseObsIntegration;

        public string GameScene
        {
            get => App.Settings.Prop.ObsGameScene;
            set => App.Settings.Prop.ObsGameScene = value;
        }

        public string LobbyScene
        {
            get => App.Settings.Prop.ObsLobbyScene;
            set => App.Settings.Prop.ObsLobbyScene = value;
        }

        public void Connect()
        {
            if (_connected) return;

            try
            {
                _obs = new ObsWebSocket();
                _obs.Connect("ws://127.0.0.1:4455", App.Settings.Prop.ObsPassword);
                _connected = true;
            }
            catch
            {
                _connected = false;
            }
        }

        public void SwitchToGameScene()
        {
            if (!_connected || _obs == null) return;
            try
            {
                _obs.Api.SetCurrentScene(GameScene);
            }
            catch { }
        }

        public void SwitchToLobbyScene()
        {
            if (!_connected || _obs == null) return;
            try
            {
                _obs.Api.SetCurrentScene(LobbyScene);
            }
            catch { }
        }

        public void Disconnect()
        {
            if (_obs != null)
            {
                _obs.Disconnect();
                _obs = null;
            }
            _connected = false;
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}
