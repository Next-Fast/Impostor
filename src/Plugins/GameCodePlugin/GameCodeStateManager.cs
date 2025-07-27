using System.Text.RegularExpressions;
using Impostor.Api.Games;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameCodePlugin;

public partial class GameCodeStateManager(ILogger<GameCodeStateManager> logger, IOptions<GameCodeConfig> config)
{
    private static readonly Regex Regex = MyRegex();
    private List<CodeState> _codes = [];
    private List<CodeState> _unusedCodes = [];

    internal void ReleaseCode(GameCode code)
    {
        var state = _codes.FirstOrDefault(used => used.Code == code);
        if (state == null)
        {
            return;
        }

        state.Used = false;
        _unusedCodes.Add(state);
    }

    internal GameCode? GetCode()
    {
        var state = config.Value.GenerateType switch
        {
            GameCodeGenerateType.Sequential => GetSequentialCode(),
            GameCodeGenerateType.Random => GetRandomCode(),
            GameCodeGenerateType.First => _unusedCodes.FirstOrDefault(),
            _ => null,
        };
        
        if (state == null)
        {
            return null;
        }

        state.Used = true;
        _unusedCodes.Remove(state);
        return state.Code;
    }

    private int _currentIndex = 1;
    private CodeState? GetSequentialCode()
    {
        if (_currentIndex > _codes.Count)
        {
            _currentIndex = 1;
        }
        
        while (_currentIndex < _codes.Count)
        {
            var state = _codes[_currentIndex];
            if (!state)
            {
                _currentIndex++;
                continue;
            }

            _currentIndex++;
            return state;
        }

        return null;
    }
    private CodeState? GetRandomCode()
    {
        if (_unusedCodes.Count == 0)
        {
            return null;
        }
        
        var index = Random.Shared.Next(_unusedCodes.Count - 1);
        return _unusedCodes[index];
    }

    internal async ValueTask LoadCodeAsync(DirectoryInfo dir)
    {
        if (!dir.Exists)
        {
            dir.Create();
            return;
        }

        var hashSet = new HashSet<GameCode>();
        foreach (var file in dir.GetFiles("*.txt", SearchOption.AllDirectories))
        {
            await using var stream = file.OpenRead();
            using var reader = new StreamReader(stream);
            var code = 0;
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                var trim = line.Trim();
                if (!Regex.IsMatch(trim))
                {
                    continue;
                }

                hashSet.Add(GameCode.From(trim.ToUpper()));
                code++;
            }

            logger.LogInformation("Load {code} codes from {file}", code, file.Name);
        }

        _codes = hashSet.Select(code => new CodeState(code)).ToList();
        _unusedCodes = _codes.ToList();
    }


    [GeneratedRegex("^(?:[a-zA-Z]{4}|[a-zA-Z]{6})$")]
    private static partial Regex MyRegex();

    public class CodeState(GameCode code)
    {
        public GameCode Code { get; } = code;
        public bool Used { get; set; }

        public static implicit operator bool(CodeState state)
        {
            return state.Used;
        }
    }
}
