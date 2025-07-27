using System.Net.WebSockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextMatchPlugin.Types;

namespace NextMatchPlugin;

public class MatchmakerService(IOptions<NextMatchConfig> config, ILogger<MatchmakerService> logger, ConnectionActioner actioner) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var configValue = config.Value;
        if (configValue.WebsocketUrl == null)
        {
            logger.LogInformation("No websocket url provided, skipping matchmaker");
            return;
        }
        
        logger.LogInformation("Starting register Actioner");
        await actioner.RegisterActionerAsync();
        
        logger.LogInformation("starting matchmaker");
        _webSocket ??= new ClientWebSocket
        {
            Options = { CollectHttpResponseDetails = true },
        };
        
        _uri = 
            configValue.WebsocketUrl.StartsWith("ws://") 
            ? new Uri(configValue.WebsocketUrl) 
            : new Uri("ws://" + configValue.WebsocketUrl);

        await base.StartAsync(cancellationToken);
    }

    private ClientWebSocket? _webSocket;
    private Uri? _uri;
    
    private int _errorCount;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_webSocket == null || _uri == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(config.Value.Token))
        {
            logger.LogInformation("Enabling auth for matchmaker");
            _webSocket.Options.SetRequestHeader("Authorization", "Bearer " + AuthHelper.GeneratePasswordHash(config.Value.Token));
        }
        

        await _webSocket.ConnectAsync(_uri, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var buffer = new byte[1024 * 4];
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                logger.LogTrace("received matchmaker message: {message}", result.MessageType);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
                    break;
                }
                
                
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }
                
                var message = System.Text.Encoding.UTF8.GetString(buffer);
                logger.LogTrace("received matchmaker type:{type} message: {message}", result.MessageType, message);
                await actioner.HandleWebSocketMessageAsync(_webSocket, message, stoppingToken);
            }
            catch (Exception e)
            {
                if (_errorCount > 10)
                {
                    logger.LogError(e, "Too many errors, shutting down matchmaker");
                    break;
                }
                
                logger.LogError(e, "Error while receiving matchmaker message");
                _errorCount++;
            }
        }
    }

    public override void Dispose()
    {
        _webSocket?.Dispose();
        base.Dispose();
    }
}
