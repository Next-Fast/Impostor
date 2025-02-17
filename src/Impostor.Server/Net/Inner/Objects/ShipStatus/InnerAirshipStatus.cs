using System.Collections.Generic;
using System.Numerics;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Inner.Objects.ShipStatus;
using Impostor.Server.Net.Inner.Objects.Systems;
using Impostor.Server.Net.Inner.Objects.Systems.ShipStatus;
using Impostor.Server.Net.State;

namespace Impostor.Server.Net.Inner.Objects.ShipStatus;

internal class InnerAirshipStatus(Game game) : InnerShipStatus(game, MapTypes.Airship), IInnerAirshipStatus
{
    public Vector2 PreSpawnLocation { get; } = new(-25f, 40f);

    public Vector2[] SpawnLocations { get; } =
    {
        new(-0.7f, 8.5f), // Brig
        new(-0.7f, -1.0f), // Engine
        new(15.5f, 0.0f), // MainHall
        new(-7.0f, -11.5f), // Kitchen
        new(20.0f, 10.5f), // Records
        new(33.5f, -1.5f), // CargoBay
    };

    public override Vector2 GetSpawnLocation(InnerPlayerControl player, int numPlayers, bool initialSpawn)
    {
        return new Vector2(-25, 40);
    }

    protected override void AddSystems(Dictionary<SystemTypes, ISystemType> systems)
    {
        base.AddSystems(systems);

        systems.Add(SystemTypes.Doors, new DoorsSystemType(Doors));
        systems.Add(SystemTypes.Comms, new HudOverrideSystemType());
        systems.Add(SystemTypes.GapRoom, new MovingPlatformBehaviour());
        systems.Add(SystemTypes.Reactor, new HeliSabotageSystemType());
        systems.Add(SystemTypes.Decontamination, new ElectricalDoors(Doors));
        systems.Add(SystemTypes.Decontamination2, new AutoDoorsSystemType(Doors));
        systems.Add(SystemTypes.Security, new SecurityCameraSystemType());
    }
}
