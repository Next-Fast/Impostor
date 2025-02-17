using System;
using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Events.Managers;
using Impostor.Api.Net;
using Impostor.Api.Net.Inner;
using Impostor.Api.Net.Messages.Rpcs;
using Impostor.Server.Events.Player;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Inner.Objects.Components;

internal partial class InnerPlayerPhysics(
    Game game,
    ILogger<InnerPlayerPhysics> logger,
    InnerPlayerControl playerControl,
    IEventManager eventManager)
    : InnerNetObject(game)
{
    private readonly ILogger<InnerPlayerPhysics> _logger = logger;

    public override ValueTask<bool> SerializeAsync(IMessageWriter writer, bool initialState)
    {
        throw new NotImplementedException();
    }

    public override ValueTask DeserializeAsync(IClientPlayer sender, IClientPlayer? target, IMessageReader reader,
        bool initialState)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask<bool> HandleRpcAsync(ClientPlayer sender, ClientPlayer? target, RpcCalls call,
        IMessageReader reader)
    {
        if (call != RpcCalls.BootFromVent && !await ValidateOwnership(call, sender))
        {
            return false;
        }

        switch (call)
        {
            case RpcCalls.EnterVent:
            case RpcCalls.ExitVent:
            {
                if (!await ValidateCanVent(call, sender, playerControl.PlayerInfo))
                {
                    return false;
                }

                int ventId;

                switch (call)
                {
                    case RpcCalls.EnterVent:
                        Rpc19EnterVent.Deserialize(reader, out ventId);
                        break;
                    case RpcCalls.ExitVent:
                        Rpc20ExitVent.Deserialize(reader, out ventId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(call), call, null);
                }

                if (Game.GameNet.ShipStatus == null)
                {
                    if (await sender.Client.ReportCheatAsync(call, CheatCategory.ProtocolExtension,
                            "Client interacted with vent on unknown map"))
                    {
                        return false;
                    }

                    break;
                }

                if (!Game.GameNet.ShipStatus.Data.Vents.TryGetValue(ventId, out var vent))
                {
                    if (await sender.Client.ReportCheatAsync(call, CheatCategory.ProtocolExtension,
                            "Client interacted with nonexistent vent"))
                    {
                        return false;
                    }

                    break;
                }

                switch (call)
                {
                    case RpcCalls.EnterVent:
                        await eventManager.CallAsync(new PlayerEnterVentEvent(Game, sender, playerControl, vent));
                        break;
                    case RpcCalls.ExitVent:
                        await eventManager.CallAsync(new PlayerExitVentEvent(Game, sender, playerControl, vent));
                        break;
                }

                break;
            }

            case RpcCalls.BootFromVent:
            {
                Rpc34BootFromVent.Deserialize(reader, out var ventId);
                break;
            }

            case RpcCalls.ClimbLadder:
                Rpc31ClimbLadder.Deserialize(reader, out var ladderId, out var lastClimbLadderSid);
                break;

            case RpcCalls.Pet:
            {
                Rpc49Pet.Deserialize(reader, out var position, out var petPosition);
                break;
            }

            case RpcCalls.CancelPet:
            {
                Rpc50CancelPet.Deserialize(reader);
                break;
            }

            default:
                return await base.HandleRpcAsync(sender, target, call, reader);
        }

        return true;
    }
}
