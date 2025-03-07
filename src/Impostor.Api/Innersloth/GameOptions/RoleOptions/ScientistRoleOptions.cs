namespace Impostor.Api.Innersloth.GameOptions.RoleOptions;

public class ScientistRoleOptions(byte version) : IRoleOptions
{
    public byte Version { get; } = version;

    public byte Cooldown { get; set; } = 15;

    public byte BatteryCharge { get; set; } = 5;

    public RoleTypes Type
    {
        get => RoleTypes.Scientist;
    }

    public void Serialize(IMessageWriter writer)
    {
        writer.Write(Cooldown);
        writer.Write(BatteryCharge);
    }

    public static ScientistRoleOptions Deserialize(IMessageReader reader, byte version)
    {
        var options = new ScientistRoleOptions(version);

        options.Cooldown = reader.ReadByte();
        options.BatteryCharge = reader.ReadByte();

        return options;
    }
}
