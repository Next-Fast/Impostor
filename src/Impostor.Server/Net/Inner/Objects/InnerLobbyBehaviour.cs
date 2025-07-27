using System;
using System.Threading.Tasks;
using Impostor.Api.Net;
using Impostor.Api.Net.Inner.Objects;
using Impostor.Server.Net.State;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Inner.Objects;

internal class InnerLobbyBehaviour : InnerNetObject, IInnerLobbyBehaviour
{
    public InnerLobbyBehaviour(Game game, ILogger<InnerLobbyBehaviour> logger) : base(game, logger)
    {
        Components.Add(this);
    }


}
