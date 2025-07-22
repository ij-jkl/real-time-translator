using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Map controllers
app.MapControllers();

app.Run();

// Helper for WebSocket proxying
static async Task ProxyWebSocket(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
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
