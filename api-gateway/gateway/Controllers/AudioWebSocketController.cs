using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Net;

namespace gateway.Controllers
{
    [ApiController]
    [Route("ws")]
    public class AudioWebSocketController : ControllerBase
    {
        [Route("audio")]
        public async Task Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("WebSocket connections only.");
                return;
            }

            using var clientSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            using var backendSocket = new ClientWebSocket();
            var backendUri = new Uri("ws://localhost:9000/ws/audio");
            await backendSocket.ConnectAsync(backendUri, HttpContext.RequestAborted);

            var clientToBackend = ProxyWebSocket(clientSocket, backendSocket, HttpContext.RequestAborted);
            var backendToClient = ProxyWebSocket(backendSocket, clientSocket, HttpContext.RequestAborted);
            await Task.WhenAny(clientToBackend, backendToClient);
        }

        private static async Task ProxyWebSocket(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];
            while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
            {
                var result = await source.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by proxy", cancellationToken);
                    break;
                }
                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }
    }
}
