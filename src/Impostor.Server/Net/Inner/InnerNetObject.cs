using System.Threading.Tasks;
using Impostor.Api.Games;
using Impostor.Api.Net;
using Impostor.Api.Net.Inner;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Inner;

internal abstract partial class InnerNetObject(Game game, ILogger logger) : GameObject, IInnerNetObject
{
    private const int HostInheritId = -2;

    public Game Game { get; } = game;

    public SpawnFlags SpawnFlags { get; internal set; }

    public uint NetId { get; internal set; }

    public int OwnerId { get; internal set; }

    IGame IInnerNetObject.Game
    {
        get => Game;
    }

    public bool IsOwnedBy(IClientPlayer player)
    {
        return OwnerId == player.Client.Id ||
               (OwnerId == HostInheritId && player.IsHost);
    }

    public virtual ValueTask<bool> SerializeAsync(IMessageWriter writer, bool initialState)
    {
        logger.LogTrace("Serialization not implemented");
        return ValueTask.FromResult(false);
    }

    public virtual ValueTask DeserializeAsync(IClientPlayer sender, IClientPlayer? target, IMessageReader reader,
        bool initialState)
    {
        logger.LogTrace("Deserialization not implemented");
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask<bool> HandleRpcAsync(ClientPlayer sender, ClientPlayer? target, RpcCalls call,
        IMessageReader reader)
    {
        return await UnregisteredCallAsync(call, sender);
    }

    internal virtual ValueTask OnSpawnAsync()
    {
        return default;
    }
}
