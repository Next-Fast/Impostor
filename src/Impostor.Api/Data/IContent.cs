using System.Threading.Tasks;

namespace Impostor.Api.Data;

public interface IContent
{
    public string Name { get; }
    
    public bool Enable { get; }
    
    public Task<string> SerialiseAsync();
    
    public Task DeserializeAsync(string data);
}
