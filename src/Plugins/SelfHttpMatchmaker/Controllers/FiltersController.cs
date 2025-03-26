using System.Net;
using System.Text.Json;
using Impostor.Api.Config;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Manager;
using Impostor.Api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker.Controllers;

[ApiController]
public class FiltersController(INetListenerManager listenerManager, IGameManager gameManager) : ControllerBase
{
    private HostServer? _hostServer;

    private HostServer HostServer
    {
        get
        {
            if (_hostServer != null)
            {
                return _hostServer;
            }

            _hostServer = HostServer.From(IPAddress.Parse(Listener.PublicIp.ResolveIp()), Listener.PublicPort);
            return _hostServer;
        }
    }

    private ListenerConfig Listener
    {
        get => listenerManager.GetAvailableListener() ?? throw new InvalidOperationException();
    }

    private static readonly List<Filters> AllFilters = Enum.GetValues<Filters>().ToList();
    
    [HttpGet("api/filters")]
    public IActionResult GetFilters()
    {
        return Ok(new PermittedFilters
        {
            // TODO: Add filters
            Filters = AllFilters,
        });
    }

    [HttpGet("api/filtertags")]
    public IActionResult GetFilterTags()
    {
        if (!Request.TryGetSingleOrDefault("lang", out var langString))
        {
            return BadRequest("No Get Lang");
        }

        var lang = (GameKeywords)uint.Parse(langString);
        var filters = gameManager.GetFilterTags(lang);
        return Ok(filters);
    }

    [HttpGet("api/games/filtered")]
    public IActionResult GetFilteredGames()
    {
        // TODO: Add filter
        if (!Request.TryGetSingleOrDefault("filter", out var content))
        {
            return BadRequest("No Get filter");
        }
        
        var set = JsonSerializer.Deserialize<GameFiltersList>(content)?.FilterSets[0];

        if (set == null)
        {
            return BadRequest("No Get filter");
        }

        var mode = set.GameMode;

        var publicGames = gameManager.Games.Where(game => game.IsPublic).ToList();
        var matchingGames = publicGames.Where(game => game.Options.GameMode == mode && FilterGame(game, set.Filters)).ToList();
        var games = matchingGames.Select(game => GameListing.FromV2(game, HostServer.Ip, HostServer.Port)).ToList();
        var res = new FindGamesListFilteredResponse
        {
            Games = games,
            Metadata = new GamesListMetadata
            {
                AllGamesCount = publicGames.Count,
                MatchingGamesCount = matchingGames.Count,
            },
        };
        return Ok(res);
    }

    private static bool FilterGame(IGame game, List<GameFilter> filters)
    {
        // TODO: Add filter
        return true;
    }
    
}
