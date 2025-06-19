using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using Microsoft.AspNetCore.Mvc;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker.Controllers;

[ApiController]
[Route("api")]
public class FiltersController(IGameManager gameManager, ListingManager listingManager, IHostServer hostServer) : ControllerBase
{
    private static readonly List<Filters> UseFilters =
    [
        Filters.Tags,
        Filters.NumImposters,
    ];
    
    

    [HttpGet("filters")]
    public IActionResult GetFilters([FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out _))
        {
            return BadRequest(result);
        }
        
        return Ok(new PermittedFilters
        {
            // TODO: Add filters
            Filters = UseFilters,
        });
    }

    [HttpGet("filtertags")]
    public IActionResult GetFilterTags([FromQuery] string lang, [FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out _))
        {
            return BadRequest(result);
        }

        var language = (GameKeywords)uint.Parse(lang);
        var filters = gameManager.GetFilterTags(language);
        return Ok(filters);
    }

    [HttpGet("games/filtered")]
    public IActionResult GetFilteredGames([FromQuery] string filter, [FromHeader] AuthenticationHeaderValue authorization)
    {
        if (!authorization.TryVerifyTokenFormHeader(out var result, out _))
        {
            return BadRequest(result);
        }
        
        if (string.IsNullOrEmpty(filter))
        {
            return BadRequest(new MatchmakerResponse(new MatchmakerError(DisconnectReason.ServerError, "filter query para not provided")));
        }

        try
        {
            var decodedFilter = HttpUtility.UrlDecode(filter);
            var filtersList = JsonSerializer.Deserialize<GameFiltersList>(decodedFilter);
            
            // filterSets wont be null. It must at least have ChatFilter and LangFilter
            // Vanilla game only builds one filterSet and InnerSloth officials only handles first one (though you can send multiple filter sets. sloths only handle the first)
            if (filtersList == null || filtersList.FilterSets.Count != 1
                                    || filtersList.FilterSets[0].Filters.Count < 2
                                    || filtersList.FilterSets[0].Filters.All(x => x.OptionType != "languages") 
                                    || filtersList.FilterSets[0].Filters.All(x => x.OptionType != "chat"))
            {
                return BadRequest(new MatchmakerResponse(new MatchmakerError(DisconnectReason.ServerError, "Invaild filterSets")));
            }
            
            var filteredGames = listingManager.FindListingsV2(HttpContext, filtersList);
            var gameListings = filteredGames.Select(game => GameListing.FromV2(game, hostServer.Ip, hostServer.Port)).ToList();

            var response = new
            {
                games = gameListings,
                metadata = new
                {
                    allGamesCount = gameManager.Games.Count(),
                    matchingGamesCount = gameListings.Count,
                },
            };

            return Ok(response);
        }
        catch (JsonException ex)
        {
            return BadRequest(new MatchmakerResponse(new MatchmakerError(DisconnectReason.ServerError, "Unable to deserialize filter json" + ex)));
        }
        catch (Exception ex)
        {
            return BadRequest(new MatchmakerResponse(new MatchmakerError(DisconnectReason.ServerError, "Unknown excpetion caught in filter" + ex)));
        }
    }
}
