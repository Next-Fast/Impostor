﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Config;
using Impostor.Api.Events.Managers;
using Impostor.Api.Extension.Events;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using Impostor.Api.Innersloth.GameOptions;
using Impostor.Api.Net;
using Impostor.Api.Net.Manager;
using Impostor.Server.Events;
using Impostor.Server.Net.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Net.Manager;

internal class GameManager : IGameManager
{
    private readonly CompatibilityConfig _compatibilityConfig;
    private readonly ICompatibilityManager _compatibilityManager;
    private readonly IEventManager _eventManager;
    private readonly IGameCodeFactory _gameCodeFactory;
    private readonly ConcurrentDictionary<int, Game> _games;
    private readonly ConcurrentDictionary<IClient, Game?> _gamesCreatedBy;
    private readonly ILogger<GameManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GameManager(
        ILogger<GameManager> logger,
        IOptions<ServerConfig> config,
        IServiceProvider serviceProvider,
        IEventManager eventManager,
        IGameCodeFactory gameCodeFactory,
        IOptions<CompatibilityConfig> compatibilityConfig,
        ICompatibilityManager compatibilityManager)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventManager = eventManager;
        _gameCodeFactory = gameCodeFactory;
        _games = new ConcurrentDictionary<int, Game>();
        _compatibilityConfig = compatibilityConfig.Value;
        _compatibilityManager = compatibilityManager;
        _gamesCreatedBy = new ConcurrentDictionary<IClient, Game?>();
    }

    IEnumerable<IGame> IGameManager.Games
    {
        get => _games.Select(kv => kv.Value);
    }

    IGame? IGameManager.Find(GameCode code)
    {
        return Find(code);
    }

    public ValueTask<IGame?> CreateAsync(IGameOptions options, GameFilterOptions filterOptions)
    {
        return CreateAsync(null, options, filterOptions);
    }

    public Game? Find(GameCode code)
    {
        _games.TryGetValue(code, out var game);
        return game;
    }

    public async ValueTask RemoveAsync(GameCode gameCode)
    {
        if (_games.TryGetValue(gameCode, out var game) && game.PlayerCount > 0)
        {
            foreach (var player in game.Players)
            {
                await player.KickAsync();
            }

            return;
        }

        if (!_games.TryRemove(gameCode, out game))
        {
            return;
        }

        _logger.LogDebug("Remove game with code {0} ({1}).", GameCodeParser.IntToGameName(gameCode), gameCode);

        await _eventManager.CallAsync(new GameDestroyedEvent(game));
    }

    public async ValueTask<IGame?> CreateAsync(IClient? owner, IGameOptions options, GameFilterOptions filterOptions)
    {
        if (owner != null && !_gamesCreatedBy.TryAdd(owner, null))
        {
            _logger.LogWarning("Connection {Name}({ClientId}) has tried to create a second game, blocked", owner.Name,
                owner.Id);
            return null;
        }

        var @event = new GameCreationEvent(owner, this, options, filterOptions);
        await _eventManager.CallAsync(@event);

        if (@event.Cancel)
        {
            return null;
        }

        var (success, game) = await TryCreateAsync(options, filterOptions, owner, @event);

        for (var i = 0; i < 10 && !success; i++)
        {
            (success, game) = await TryCreateAsync(options, filterOptions, owner, @event);
        }

        if (owner != null)
        {
            _gamesCreatedBy[owner] = game;
        }

        if (!success || game == null)
        {
            throw new ImpostorException("Could not create new game"); // TODO: Fix generic exception.
        }

        return game;
    }

    private async ValueTask<(bool Success, Game? Game)> TryCreateAsync(IGameOptions options,
        GameFilterOptions filterOptions, IClient? owner, GameCreationEvent? creationEvent = null)
    {
        var gameCode = await _gameCodeFactory.CreateAsync(creationEvent);
        var game = ActivatorUtilities.CreateInstance<Game>(_serviceProvider, gameCode, options, filterOptions);

        if (!_games.TryAdd(gameCode, game))
        {
            return (false, null);
        }

        _logger.LogDebug("Created game with code {0}.", game.Code);

        await _eventManager.CallAsync(new GameCreatedEvent(game, owner));

        return (true, game);
    }

    internal async ValueTask OnClientDisconnectAsync(IClient client)
    {
        if (_gamesCreatedBy.TryRemove(client, out var game) &&
            game is { PlayerCount: 0, GameState: not GameStates.Destroyed })
        {
            _logger.LogWarning("Client {Name}({ClientId}) left empty game open when disconnecting", client.Name,
                client.Id);
            await RemoveAsync(game.Code);
        }
    }
}
