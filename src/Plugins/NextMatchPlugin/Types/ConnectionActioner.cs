using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Impostor.Api.Events;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NextMatchPlugin.Types;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class RegisterActionerAttribute(ConnectionType type) : Attribute
{
    public ConnectionType Type => type;
}

public interface IActioner
{
    public Task HandleActionAsync(ConnectionAction action, IConnectionEvent connectionEvent);
}

public class ConnectionActioner(ILogger<ConnectionActioner> logger, IServiceProvider provider)
{
    private Dictionary<ConnectionType, IActioner> Actioners { get; } = [];

    public Task RegisterActionerAsync()
    {
        var types = typeof(ConnectionActioner)
            .Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsDefined(typeof(RegisterActionerAttribute), true) && t.IsDefined(typeof(IActioner), true));

        foreach (var type in types)
        {
            try
            {
                var actioner = (IActioner)ActivatorUtilities.CreateInstance(provider, type);
                var attr = type.GetCustomAttribute<RegisterActionerAttribute>();
                if (attr == null)
                {
                    continue;
                }
                Actioners[attr.Type] = actioner;
                logger.LogDebug("Registered actioner {0} for {1}", type.Name, attr.Type);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to register actioner {0}", type.Name);
            }
        }
        
        return Task.CompletedTask;
    }

    public CancellationToken? StopingToken { get; private set; }
    public ClientWebSocket? WebSocket { get; private set; }
    public async Task HandleWebSocketMessageAsync(ClientWebSocket? socket, string text, CancellationToken token)
    {
        WebSocket = socket;
        StopingToken = token;

        var @event = JsonSerializer.Deserialize<IConnectionEvent>(text);
        if (@event == null)
        {
            logger.LogTrace("Failed to deserialize event {0}", text);
            return;
        }
        

        if (@event.Type == ConnectionType.heartbeat)
        {
            if (@event.Action != ConnectionAction.Response)
            {
                logger.LogTrace("Heartbeat action is not request");
                return;
            }

            var res = @event.ToResponse();
            if (res.Data is not Dictionary<string, string> heartbeat)
            {
                logger.LogTrace("Failed to deserialize heartbeat");
                return;
            }

            logger.LogTrace("Custom Heartbeat received Time:{0} State:{1}", heartbeat["time"], heartbeat["state"]);
            return;
        }

        if (@event.Action == ConnectionAction.WaitResponse)
        {
            if (_pendingRequests.TryRemove(@event.Id, out var tcs))
            {
                tcs.TrySetResult(@event.ToResponse());
            }
            else
            {
                logger.LogTrace("No pending request found for {0}", @event.Id);
            }
            
            return;
        }

        if (Actioners.TryGetValue(@event.Type, out var actioner))
        {
            await actioner.HandleActionAsync(@event.Action, @event);
            return;
        }

        logger.LogTrace("No actioner found for {0}", @event.Type);
    }

    public async Task SendMessageAsync(byte[] buffer, CancellationToken? cancellationToken = null)
    {
        if (WebSocket == null)
        {
            logger.LogTrace("WebSocket or StopingToken is null");
            return;
        }

        var token = cancellationToken ?? StopingToken ?? CancellationToken.None;
        await WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, token);
    }

    public Task SendMessageAsync(string content, CancellationToken? cancellationToken = null)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return SendMessageAsync(bytes, cancellationToken);
    }

    public Task SendJsonAsync<T>(T content, CancellationToken? cancellationToken = null)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(content);
        return SendMessageAsync(jsonBytes, cancellationToken);
    }
    
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ConnectionResponse>> _pendingRequests = new();

    public async ValueTask<ConnectionResponse?> SendAndWaitResponseAsync(ConnectionRequest request, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        var tcs = new TaskCompletionSource<ConnectionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pendingRequests.TryAdd(request.Id, tcs))
        {
            logger.LogError("Request id {0} already exists", request.Id);
            return null;
        }

        try
        {
            await SendJsonAsync(request, cancellationToken);
            using var timeoutCts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
            var token = cancellationToken ?? StopingToken ?? CancellationToken.None;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
            return await tcs.Task.WaitAsync(linkedCts.Token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send request {0}", request.Id);
            _pendingRequests.TryRemove(request.Id, out _);
            return null;
        }
    }
}
