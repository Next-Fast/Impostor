using System.Text.Json.Serialization;
using Impostor.Api.Config;
using Impostor.Api.Utils;

namespace GameCodePlugin;

public class GameCodeConfig : IConfigSet
{
    public const string Section = "GameCode";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GameCodeGenerateType GenerateType { get; set; } = GameCodeGenerateType.Random;
    public string GameCodeDir { get; set; } = "{Root}/GameCode";
    public string SectionName => Section;
    public void Set(string key, IArgUtils value)
    {
        if (key != "GenerateType")
        {
            return;
        }

        var type = value.GetEnumArg(0, GenerateType);
        GenerateType = type;
    }
}


public enum GameCodeGenerateType
{
    // 顺序
    Sequential,
    // 随机
    Random,
    // 第一个
    First,
}
