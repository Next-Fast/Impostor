using System.Net.Http.Headers;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker.Controllers;

/// <summary>
///     This controller has method to get a list of public games, join by game and create new games.
/// </summary>
[Route("/api/games")]
[ApiController]
public sealed class GamesController(
    IGameManager gameManager,
    ListingManager listingManager,
    IOptions<SelfHttpConfig> config,
    IHostServer hostServer) : ControllerBase
{
    /// <summary>
    ///     Get a list of active games.
    /// </summary>
    /// <param name="mapId">Maps that are requested.</param>
    /// <param name="lang">Preferred chat language.</param>
    /// <param name="numImpostors">Amount of impostors. 0 is any.</param>
    /// <param name="authorization">Authorization header containing the matchmaking token.</param>
    /// <returns>An array of game listings.</returns>
    [HttpGet]
    public IActionResult Index(int mapId, GameKeywords lang, int numImpostors,
        [FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out var token))
        {
            return BadRequest(result);
        }

        var clientVersion = new GameVersion(token.Content.ClientVersion);

        var listings = listingManager.FindListings(HttpContext, mapId, numImpostors, lang, clientVersion);

        return Ok(listings.Select(n => GameListing.From(n, hostServer.Ip, hostServer.Port)));
    }

    /// <summary>
    ///     Get the address a certain game is hosted at.
    /// </summary>
    /// <param name="gameId">The id of the game that should be retrieved.</param>
    /// <returns>The server this game is hosted on.</returns>
    [HttpPost]
    public IActionResult Post(int gameId)
    {
        var code = new GameCode(gameId);
        var game = gameManager.Find(code);

        // If the game was not found, print an error message.
        if (game == null)
        {
            return NotFound(new MatchmakerResponse(new MatchmakerError(DisconnectReason.GameNotFound)));
        }

        return Ok(hostServer);
    }

    /// <summary>
    ///     Get the address to host a new game on.
    /// </summary>
    /// <returns>The address of this server.</returns>
    [HttpPut]
    public IActionResult Put([FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out _) && config.Value.PutTokenAuth)
        {
            return BadRequest(result);
        }

        return Ok(hostServer);
    }

    [HttpGet("{gameId:int}")]
    public IActionResult FindGameInfo(int gameId, [FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out _))
        {
            return BadRequest(result);
        }

        var code = GameCode.From(gameId);
        var game = gameManager.Find(code);
        if (game == null)
        {
            return NotFound(new MatchmakerResponse(new MatchmakerError(DisconnectReason.GameNotFound)));
        }

        var listing = GameListing.FromV2(game, hostServer.Ip, hostServer.Port);
        var res = new FindGameByCodeResponse
        {
            Errors = [],
            Game = listing,
        };
        return Ok(res);
    }
}
