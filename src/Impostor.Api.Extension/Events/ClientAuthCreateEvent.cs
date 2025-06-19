using Impostor.Api.Events;
using Impostor.Api.Extension.Net;

namespace Impostor.Api.Extension.Events;

public class ClientAuthCreateEvent(ClientAuthInfo authInfo) : IEvent
{
    public ClientAuthInfo DefaultAuthInfo { get; } = authInfo;
    public EventTypeResult Status { get; set; } = new(EventResultType.Success);

    public EventOutcome<ClientAuthInfo>? Result { get; set; }
}
