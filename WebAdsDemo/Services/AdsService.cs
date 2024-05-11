using System.Xml.Linq;
using TwinCAT.Ads;

namespace WebAdsDemo.Services
{
    public interface IAdsService
    {
        T ReadValue<T>(string varName) where T : new();
        void WriteValue<T>(string varName, T value) where T : notnull;
        uint SetNotification<T>(string name, EventHandler<AdsNotificationExEventArgs> handler);
        void UnsetNotification(uint notificationHandle, EventHandler<AdsNotificationExEventArgs> handler);
    }

    public class AdsService : IAdsService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly AdsClient _client = new AdsClient();
        private readonly string _amsNetId;
        private readonly int _port;

        public bool IsConnected { get => _client.IsConnected; }

        private void Connect()
        {
            if (!_client.IsConnected)
                _client.Connect(_amsNetId, _port);
        }

        private void Disconnect()
        {
            if (_client.IsConnected)
                _client.Disconnect();
        }

        public AdsService(string amsNetId, int port)
        {
            _logger = LoggerFactory.Create(opt => { }).CreateLogger<AdsService>();
            _amsNetId = amsNetId;
            _port = port;
        }

        public T ReadValue<T>(string varName) where T : new()
        {
            try
            {
                T valueToRead = new T();

                Connect();

                if (_client.IsConnected)
                    valueToRead = (T)_client.ReadValue(varName, typeof(T));

                return valueToRead;
            }
            catch
            {
                _logger.LogError($"Unable to read value from {varName}.");
                throw;
            }
        }

        public void WriteValue<T>(string varName, T value) where T : notnull
        {
            try
            {
                Connect();

                if (!_client.IsConnected)
                    return;

                _client.WriteValue(varName, value);
            }
            catch
            {
                _logger.LogError($"Unable to write value to {varName}.");
                throw;
            }
        }

        public uint SetNotification<T>(string name, EventHandler<AdsNotificationExEventArgs> handler)
        {
            try
            {
                Connect();

                if (!_client.IsConnected)
                    return 0;

                NotificationSettings ns = new NotificationSettings(AdsTransMode.OnChange, 200, 0);

                _client.AdsNotificationEx += handler;
                uint ntHandle = _client.AddDeviceNotificationEx(name, ns, null, typeof(T));

                return ntHandle;
            }
            catch
            {
                _logger.LogError($"Unable to set notification for {name}.");
                throw;
            }
        }

        public void UnsetNotification(uint notificationHandle, EventHandler<AdsNotificationExEventArgs> handler)
        {
            try
            {
                if (!_client.IsConnected)
                    return;

                _client.DeleteDeviceNotification(notificationHandle);
                _client.AdsNotificationEx -= handler;
            }
            catch
            {
                _logger.LogError($"Unable to unset notification for handle={notificationHandle}.");
                throw;
            }
        }


        public void Dispose()
        {
            Disconnect();
            _client.Dispose();
        }
    }
}
