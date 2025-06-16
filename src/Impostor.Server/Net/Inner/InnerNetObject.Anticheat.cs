using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Impostor.Server.Net.Inner.Objects;

namespace Impostor.Server.Net.Inner;

internal abstract partial class InnerNetObject
{
    protected async ValueTask<bool> ValidateOwnershipAsync(CheatContext context, IClientPlayer sender)
    {
        if (sender.IsOwner(this))
        {
            return true;
        }

        return !await sender.Client.ReportCheatAsync(context, CheatCategory.Ownership,
            $"Failed ownership check on {GetType().Name}");
    }

    protected async ValueTask<bool> ValidateHostAsync(CheatContext context, IClientPlayer sender)
    {
        if (sender.IsHost)
        {
            return true;
        }

        return !await sender.Client.ReportCheatAsync(context, CheatCategory.MustBeHost, "Failed host check");
    }

    protected async ValueTask<bool> ValidateTargetAsync(CheatContext context, IClientPlayer sender, IClientPlayer? target)
    {
        if (target != null)
        {
            return true;
        }

        return !await sender.Client.ReportCheatAsync(context, CheatCategory.Target, "Failed target check");
    }

    protected async ValueTask<bool> ValidateBroadcastAsync(CheatContext context, IClientPlayer sender, IClientPlayer? target)
    {
        if (target == null)
        {
            return true;
        }

        return !await sender.Client.ReportCheatAsync(context, CheatCategory.Target, "Failed broadcast check");
    }

    protected async ValueTask<bool> ValidateCmdAsync(CheatContext context, IClientPlayer sender, IClientPlayer? target)
    {
        if (target is { IsHost: true })
        {
            return true;
        }

        return !await sender.Client.ReportCheatAsync(context, CheatCategory.Target, "Failed cmd check");
    }

    protected async ValueTask<bool> ValidateImpostorAsync(CheatContext context, IClientPlayer sender,
        InnerPlayerInfo? playerInfo, bool value = true)
    {
        if (playerInfo == null)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.InvalidObject, "Couldn't check if Impostor, playerInfo not set"))
            {
                return false;
            }
        }
        else if (playerInfo.IsImpostor != value)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.Role, "Failed impostor check"))
            {
                return false;
            }
        }

        return true;
    }

    protected async ValueTask<bool> ValidateCanVentAsync(CheatContext context, IClientPlayer sender,
        InnerPlayerInfo? playerInfo, bool value = true)
    {
        if (playerInfo == null)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.InvalidObject, "Couldn't check if can vent, playerInfo not set"))
            {
                return false;
            }
        }
        else if (playerInfo.CanVent != value)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.Role, "Failed can vent check"))
            {
                return false;
            }
        }

        return true;
    }

    protected async ValueTask<bool> ValidateRoleAsync(CheatContext context, IClientPlayer sender, InnerPlayerInfo? playerInfo,
        RoleTypes role)
    {
        if (playerInfo == null)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.InvalidObject, "Couldn't check if role, playerInfo not set"))
            {
                return false;
            }
        }
        else if (playerInfo.RoleType != role)
        {
            if (await sender.Client.ReportCheatAsync(context, CheatCategory.Role, $"Failed role = {role} check"))
            {
                return false;
            }
        }

        return true;
    }

    private static async ValueTask<bool> UnregisteredCallAsync(CheatContext context, IClientPlayer sender)
    {
        return !await sender.Client.ReportCheatAsync(context, CheatCategory.ProtocolExtension,
            "Client sent unregistered call");
    }
}
