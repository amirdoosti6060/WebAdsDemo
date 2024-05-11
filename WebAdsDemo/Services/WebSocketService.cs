using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebAdsDemo.Services
{
    public class WebSocketService
    {
        private ConcurrentDictionary<string, WebSocket> _clients = 
            new ConcurrentDictionary<string, WebSocket>();

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
        }

        private void RemoveWebSocketClient(string clientId)
        {
            _clients.TryRemove(clientId, out _);
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
                    else
                    {
                        // Broadcast message to all connected clients (optional)
                        await BroadcastMessage(buffer, result.Count);
                    }
                }
            }
            catch (WebSocketException ex)
            {
                // Handle WebSocket exceptions
                Console.WriteLine($"WebSocket communication error for client {clientId}: {ex.Message}");
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

        private async Task BroadcastMessage(byte[] buffer, int count)
        {
            foreach (var client in _clients)
            {
                try
                {
                    if (client.Value.State == WebSocketState.Open)
                    {
                        await client.Value.SendAsync(new ArraySegment<byte>(buffer, 0, count), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception while sending message to client
                    Console.WriteLine($"Error broadcasting message to client {client.Key}: {ex.Message}");
                }
            }
        }
    }
}
