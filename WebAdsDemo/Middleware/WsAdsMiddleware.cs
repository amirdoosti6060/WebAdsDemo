using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TwinCAT.Ads;
using WebAdsDemo.Services;

namespace WebAdsDemo.Middleware
{
    public class WsAdsMiddleware
    {
        private class AdsState
        {
            public bool bStatus { get; set; }
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<WsAdsMiddleware> _logger;
        private readonly IAdsService _adsService;
        private ConcurrentDictionary<string, WebSocket> _clients =
            new ConcurrentDictionary<string, WebSocket>();
        private readonly EventHandler<AdsNotificationExEventArgs> _adsPulseEventHandler;
        private AdsState _adsState = new AdsState();
        private static object _lock = new object();

        public WsAdsMiddleware(RequestDelegate next, ILogger<WsAdsMiddleware> logger, IAdsService adsService)
        {
            _next = next;
            _logger = logger;
            _adsService = adsService;

            _adsPulseEventHandler = new EventHandler<AdsNotificationExEventArgs>(AdsPulseNotificationHandler);
            _adsService.SetNotification<bool>("MAIN.bPulse", _adsPulseEventHandler);
        }

        private async void AdsPulseNotificationHandler(object? sender, AdsNotificationExEventArgs e)
        {
            var message = "";
            lock(_lock)
            {
                _adsState.bStatus = (bool) e.Value;
                _logger.LogInformation($"MAIN.bPulse = {_adsState.bStatus}");
                message = JsonSerializer.Serialize<AdsState>(_adsState);
            }
            await BroadcastMessage(message);
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == "/")
            {
                await HandleWebSocket(context);
            }
            else
            {
                if (_next != null)
                    await _next(context);
            }
        }

        public async Task HandleWebSocket(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                AddWebSocketClient(context.Connection.Id, webSocket);
                await HandleWebSocketCommunication(context.Connection.Id, webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private void AddWebSocketClient(string clientId, WebSocket webSocket)
        {
            _clients.TryAdd(clientId, webSocket);
            _logger.LogInformation($"Client {clientId} is added.");
        }

        private void RemoveWebSocketClient(string clientId)
        {
            _clients.TryRemove(clientId, out _);
            _logger.LogInformation($"Client {clientId} is removed.");
        }

        private async Task HandleWebSocketCommunication(string clientId, WebSocket webSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    // Handle received message from the client
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Handle WebSocket close message
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocket connection closed", CancellationToken.None);
                        RemoveWebSocketClient(clientId);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation($"Received data from {clientId}: {message}.");

                        try
                        {
                            var rcvState = JsonSerializer.Deserialize<AdsState>(message);

                            _adsState.bStatus = rcvState!.bStatus;
                            message = JsonSerializer.Serialize<AdsState>(_adsState);

                            // Broadcast message to all connected clients (optional)
                            await BroadcastMessage(message);
                        }
                        catch
                        {
                            _logger.LogError("Bad request! Input should be a serialized json with specific fields!");
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                // Handle WebSocket exceptions
                _logger.LogError($"WebSocket communication error for client {clientId}: {ex.Message}");
                RemoveWebSocketClient(clientId);
            }
            finally
            {
                if (webSocket != null && webSocket.State != WebSocketState.Closed)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocket connection closed", CancellationToken.None);
                    RemoveWebSocketClient(clientId);
                }
            }
        }

        private async Task BroadcastMessage(string message)
        {
            foreach (var client in _clients)
            {
                try
                {
                    if (client.Value.State == WebSocketState.Open)
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        await client.Value.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        _logger.LogInformation($"Sent data to {client.Key}: {message}.");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception while sending message to client
                    _logger.LogError($"Error broadcasting message to client {client.Key}: {ex.Message}");
                }
            }
        }

    }
}
