using System.Text.Json.Serialization;
using Impostor.Api.Utils;

namespace Impostor.Api.Config;

public interface IConfigSet
{
    [JsonIgnore]
    public string SectionName { get; }
    
    public void Set(string key, IArgUtils value);
}
