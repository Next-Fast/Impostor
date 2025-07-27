using System;

namespace Impostor.Server.Net.Inner.Objects.Systems.ShipStatus;

public class HudOverrideSystemType : ISystemType, IActivatable
{
    public bool IsActive { get; private set; }

    public void Serialize(IMessageWriter writer, bool initialState)
    {

    }

    public void Deserialize(IMessageReader reader, bool initialState)
    {
        IsActive = reader.ReadBoolean();
    }
}
