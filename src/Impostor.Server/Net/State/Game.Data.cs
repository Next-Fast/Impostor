using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Inner;
using Impostor.Api.Unity;
using Impostor.Server.Events;
using Impostor.Server.Events.Meeting;
using Impostor.Server.Events.Player;
using Impostor.Server.Net.Inner;
using Impostor.Server.Net.Inner.Objects;
using Impostor.Server.Net.Inner.Objects.Components;
using Impostor.Server.Net.Inner.Objects.GameManager;
using Impostor.Server.Net.Inner.Objects.ShipStatus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.State;

internal partial class Game
{
    /// <summary>
    ///     Used for global object, spawned by the host.
    /// </summary>
    private const int InvalidClient = -2;

    /// <summary>
    ///     Used internally to set the OwnerId to the current ClientId.
    ///     i.e: <code>ownerId = ownerId == -3 ? this.ClientId : ownerId;</code>
    /// </summary>
    private const int CurrentClient = -3;

    /// <summary>
    ///     Used to list objects that are managed by the game server.
    /// </summary>
    private const int ServerOwned = -4;

    /// <summary>
    ///     The first NetId that is considered as a server owned Network ID that the client will not allocate by default.
    /// </summary>
    private const int MinServerNetId = 100000;

    private static readonly Dictionary<uint, Type> SpawnableObjects = new()
    {
        [0] = typeof(InnerSkeldShipStatus),
        [1] = typeof(InnerMeetingHud),
        [2] = typeof(InnerLobbyBehaviour),
        [4] = typeof(InnerPlayerControl),
        [5] = typeof(InnerMiraShipStatus),
        [6] = typeof(InnerPolusShipStatus),
        [7] = typeof(InnerDleksShipStatus),
        [8] = typeof(InnerAirshipStatus),
        [9] = typeof(InnerHideAndSeekManager),
        [10] = typeof(InnerNormalGameManager),
        [11] = typeof(InnerPlayerInfo),
        [12] = typeof(InnerVoteBanSystem),
        [13] = typeof(InnerFungleShipStatus),
    };

    private static readonly Dictionary<Type, uint> SpawnableObjectIds =
        SpawnableObjects.ToDictionary(i => i.Value, i => i.Key);

    private readonly List<InnerNetObject> _allObjects = new();

    private readonly Dictionary<uint, InnerNetObject> _allObjectsFast = new();

    private uint _nextNetId = MinServerNetId;

    public T? FindObjectByNetId<T>(uint netId)
        where T : IInnerNetObject
    {
        if (_allObjectsFast.TryGetValue(netId, out var obj))
        {
            return (T)(IInnerNetObject)obj;
        }

        return default;
    }

    public async ValueTask<bool> HandleGameDataAsync(IMessageReader parent, ClientPlayer sender, bool toPlayer)
    {
        // Find target player.
        ClientPlayer? target = null;

        if (toPlayer)
        {
            var targetId = parent.ReadPackedInt32();
            if (!TryGetPlayer(targetId, out target))
            {
                logger.LogWarning("Player {0} tried to send GameData to unknown player {1}.", sender.Client.Id,
                    targetId);
                return false;
            }

            logger.LogTrace("Received GameData for target {0}.", targetId);
        }

        // Parse GameData messages.
        while (parent.Position < parent.Length)
        {
            if (sender.Client.Player == null)
            {
                // Disconnect handler was probably invoked, cancel the rest.
                return false;
            }

            if (toPlayer && (target == null || !_players.ContainsKey(target.Client.Id)))
            {
                // target is disconnected, cancel the rest.
                return false;
            }

            using var reader = parent.ReadMessage();
            try
            {
                if (!await HandleGameDataInnerAsync(reader, sender, toPlayer, target))
                {
                    parent.RemoveMessage(reader);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to handle GameData message.");
                parent.RemoveMessage(reader);
            }
        }

        return true;
    }

    private async ValueTask<bool> HandleGameDataInnerAsync(IMessageReader reader, ClientPlayer sender, bool toPlayer,
        ClientPlayer? target)
    {
        switch (reader.Tag)
        {
            case GameDataTag.DataFlag:
            {
                var netId = reader.ReadPackedUInt32();
                if (_allObjectsFast.TryGetValue(netId, out var obj))
                {
                    await obj.DeserializeAsync(sender, target, reader, false);
                }
                else
                {
                    logger.LogWarning("Received DataFlag for unregistered NetId {0}.", netId);
                }

                break;
            }

            case GameDataTag.RpcFlag:
            {
                var netId = reader.ReadPackedUInt32();
                var rpc = reader.ReadByte();
                if (_allObjectsFast.TryGetValue(netId, out var obj))
                {
                    var messageEvent = new GameMessageEvent(obj, rpc, reader);
                    await eventManager.CallAsync(messageEvent);
                    if (messageEvent.HasBreak)
                    {
                        break;
                    }

                    if (messageEvent.HandleRpc != null && messageEvent.HandleRpc.Invoke(sender, target))
                    {
                        return false;
                    }

                    if (!await obj.HandleRpcAsync(sender, target, (RpcCalls)rpc, reader))
                    {
                        return false;
                    }
                }
                else
                {
                    logger.LogWarning("Received RpcFlag for unregistered NetId {0}.", netId);
                }

                break;
            }

            case GameDataTag.SpawnFlag:
            {
                // Only the host is allowed to spawn objects.
                if (!sender.IsHost)
                {
                    if (await sender.Client.ReportCheatAsync(new CheatContext(nameof(GameDataTag.SpawnFlag)),
                            CheatCategory.MustBeHost, "Tried to send SpawnFlag as non-host."))
                    {
                        return false;
                    }
                }

                var objectId = reader.ReadPackedUInt32();
                if (SpawnableObjects.TryGetValue(objectId, out var spawnableObjectType))
                {
                    var innerNetObject =
                        (InnerNetObject)ActivatorUtilities.CreateInstance(serviceProvider, spawnableObjectType,
                            this);
                    var ownerClientId = reader.ReadPackedInt32();

                    innerNetObject.SpawnFlags = (SpawnFlags)reader.ReadByte();

                    var components = innerNetObject.GetComponentsInChildren<InnerNetObject>();
                    var componentsCount = reader.ReadPackedInt32();

                    if (componentsCount != components.Count)
                    {
                        logger.LogError(
                            "Children didn't match for spawnable {0}, name {1} ({2} != {3})",
                            objectId,
                            innerNetObject.GetType().Name,
                            componentsCount,
                            components.Count);
                        return false;
                    }

                    logger.LogDebug(
                        "Spawning {0} components, SpawnFlags {1}",
                        innerNetObject.GetType().Name,
                        innerNetObject.SpawnFlags);

                    for (var i = 0; i < componentsCount; i++)
                    {
                        var obj = components[i];

                        obj.NetId = reader.ReadPackedUInt32();
                        obj.OwnerId = ownerClientId;

                        logger.LogDebug(
                            "- {0}, NetId {1}, OwnerId {2}",
                            obj.GetType().Name,
                            obj.NetId,
                            obj.OwnerId);

                        if (!AddNetObject(obj))
                        {
                            logger.LogTrace("Failed to AddNetObject, it already exists.");

                            obj.NetId = uint.MaxValue;
                            break;
                        }

                        using var readerSub = reader.ReadMessage();
                        if (readerSub.Length > 0)
                        {
                            await obj.DeserializeAsync(sender, target, readerSub, true);
                        }

                        await OnSpawnAsync(sender, obj);
                    }

                    return false;
                }

                logger.LogWarning("Couldn't find spawnable object {0}.", objectId);
                break;
            }

            // Only the host is allowed to despawn objects.
            case GameDataTag.DespawnFlag:
            {
                var netId = reader.ReadPackedUInt32();
                if (_allObjectsFast.TryGetValue(netId, out var obj))
                {
                    if (sender.Client.Id != obj.OwnerId && !sender.IsHost)
                    {
                        logger.LogWarning(
                            "Player {0} ({1}) tried to send DespawnFlag for {2} but was denied.",
                            sender.Client.Name,
                            sender.Client.Id,
                            netId);
                        return false;
                    }

                    RemoveNetObject(obj);
                    await OnDestroyAsync(obj);
                    logger.LogDebug("Destroyed InnerNetObject {0} ({1}), OwnerId {2}", obj.GetType().Name, netId,
                        obj.OwnerId);
                }
                else
                {
                    logger.LogDebug(
                        "Player {0} ({1}) sent DespawnFlag for unregistered NetId {2}.",
                        sender.Client.Name,
                        sender.Client.Id,
                        netId);
                }

                break;
            }

            case GameDataTag.SceneChangeFlag:
            {
                // Sender is only allowed to change his own scene.
                var clientId = reader.ReadPackedInt32();
                var scene = reader.ReadString();

                if (clientId != sender.Client.Id)
                {
                    logger.LogWarning(
                        "Player {0} ({1}) tried to send SceneChangeFlag for another player.",
                        sender.Client.Name,
                        sender.Client.Id);
                    return false;
                }

                // According to game assembly, sender is only allowed to send OnlineGame.
                if (scene != "OnlineGame")
                {
                    logger.LogWarning(
                        "Player {PlayerName} ({ClientId}) tried to send SceneChangeFlag with disallowed scene \"{Scene}\".",
                        sender.Client.Name,
                        sender.Client.Id,
                        scene);
                    return false;
                }

                sender.Scene = scene;

                logger.LogTrace("> Scene {0} to {1}", clientId, sender.Scene);

                await SyncServerObjectsAsync(sender);
                await SpawnPlayerInfoAsync(sender);

                break;
            }

            case GameDataTag.ReadyFlag:
            {
                var clientId = reader.ReadPackedInt32();

                if (clientId != sender.Client.Id)
                {
                    logger.LogWarning(
                        "Player {0} ({1}) tried to send ReadyFlag for another player.",
                        sender.Client.Name,
                        sender.Client.Id);
                    return false;
                }

                logger.LogTrace("> IsReady {0}", clientId);
                break;
            }

            case GameDataTag.ConsoleDeclareClientPlatformFlag:
            {
                var clientId = reader.ReadPackedInt32();
                var platform = (RuntimePlatform)reader.ReadPackedInt32();

                if (clientId != sender.Client.Id)
                {
                    if (await sender.Client.ReportCheatAsync(
                            new CheatContext(nameof(GameDataTag.ConsoleDeclareClientPlatformFlag)),
                            CheatCategory.Ownership, "Client sent info with wrong client id"))
                    {
                        return false;
                    }
                }

                sender.Platform = platform;

                break;
            }

            default:
            {
                logger.LogWarning("Bad GameData tag {0}", reader.Tag);
                break;
            }
        }

        return true;
    }

    private async ValueTask OnSpawnAsync(ClientPlayer sender, InnerNetObject netObj)
    {
        switch (netObj)
        {
            case InnerGameManager innerGameManager:
            {
                GameNet.GameManager = innerGameManager;
                break;
            }

            case InnerLobbyBehaviour lobby:
            {
                GameNet.LobbyBehaviour = lobby;
                break;
            }

            case InnerPlayerInfo playerInfo:
            {
                if (!GameNet.GameData.AddPlayer(playerInfo))
                {
                    logger.LogWarning(
                        "Could not add PlayerInfo for playerId {PlayerId} with NetId {newId}, already have NetId {oldNetId}",
                        playerInfo.PlayerId,
                        playerInfo.NetId,
                        GameNet.GameData.GetPlayerById(playerInfo.PlayerId)?.NetId);
                }

                break;
            }

            case InnerVoteBanSystem voteBan:
            {
                GameNet.VoteBan = voteBan;
                break;
            }

            case InnerShipStatus shipStatus:
            {
                GameNet.ShipStatus = shipStatus;
                break;
            }

            case InnerPlayerControl control:
            {
                // Hook up InnerPlayerControl <-> IClientPlayer.
                if (TryGetPlayer(control.OwnerId, out var player))
                {
                    player.Character = control;
                    player.DisableSpawnTimeout();
                }
                else
                {
                    await sender.Client.ReportCheatAsync(new CheatContext(nameof(GameDataTag.SpawnFlag)),
                        CheatCategory.GameFlow, "Failed to find player that spawned the InnerPlayerControl");
                }

                // Hook up InnerPlayerControl <-> InnerPlayerControl.PlayerInfo.
                var playerInfo = GameNet.GameData.GetPlayerById(control.PlayerId);

                if (playerInfo != null)
                {
                    playerInfo.Controller = control;
                    control.PlayerInfo = playerInfo;
                }

                if (player != null)
                {
                    await eventManager.CallAsync(new PlayerSpawnedEvent(this, player, control));
                }

                break;
            }

            case InnerMeetingHud meetingHud:
            {
                foreach (var player in _players.Values)
                {
                    if (GameNet.ShipStatus != null)
                    {
                        await player.Character!.NetworkTransform.SetPositionAsync(player,
                            GameNet.ShipStatus.GetSpawnLocation(player.Character, PlayerCount, false));
                    }
                }

                await eventManager.CallAsync(new MeetingStartedEvent(this, meetingHud));
                break;
            }
        }

        await netObj.OnSpawnAsync();
    }

    private async ValueTask OnDestroyAsync(InnerNetObject netObj)
    {
        switch (netObj)
        {
            case InnerLobbyBehaviour:
            {
                GameNet.LobbyBehaviour = null;
                break;
            }

            case InnerVoteBanSystem:
            {
                GameNet.VoteBan = null;
                break;
            }

            case InnerShipStatus:
            {
                GameNet.ShipStatus = null;
                break;
            }

            case InnerPlayerInfo playerInfo:
            {
                if (GameState != GameStates.Started && GameState != GameStates.Starting)
                {
                    GameNet.GameData.RemovePlayer(playerInfo.PlayerId);
                }

                break;
            }

            case InnerPlayerControl control:
            {
                // Remove InnerPlayerControl <-> IClientPlayer.
                if (TryGetPlayer(control.OwnerId, out var player))
                {
                    player.Character = null;
                    await eventManager.CallAsync(new PlayerDestroyedEvent(this, player, control));
                }

                break;
            }
        }
    }

    private async ValueTask SyncServerObjectsAsync(ClientPlayer sender)
    {
        foreach (var obj in _allObjectsFast.Values)
        {
            if (obj.OwnerId == ServerOwned)
            {
                logger.LogTrace("Syncing {Type} {NetId}", obj.GetType(), obj.NetId);
                await SendObjectSpawnAsync(obj, sender.Client.Id);
            }
        }
    }

    private async ValueTask SpawnPlayerInfoAsync(ClientPlayer sender)
    {
        // Hosts spawn PlayerInfo objects if they requested authority
        if (IsHostAuthoritive)
        {
            return;
        }

        // Only spawn a new PlayerInfo if one has not yet been spawned
        if (GameNet.GameData.PlayersByClientId.ContainsKey(sender.Client.Id))
        {
            return;
        }

        var playerInfo =
            (InnerPlayerInfo)ActivatorUtilities.CreateInstance(serviceProvider, typeof(InnerPlayerInfo), this);
        playerInfo.SpawnFlags = SpawnFlags.None;
        playerInfo.NetId = _nextNetId++;
        playerInfo.OwnerId = ServerOwned;
        playerInfo.ClientId = sender.Client.Id;
        playerInfo.PlayerId = GameNet.GameData.GetNextAvailablePlayerId();

        if (!AddNetObject(playerInfo))
        {
            logger.LogError("Couldn't spawn PlayerInfo for {Name} ({ClientId})", sender.Client.Name, sender.Client.Id);
            playerInfo.NetId = uint.MaxValue;
            return;
        }

        logger.LogTrace("Spawning PlayerInfo (netId {Netid})", playerInfo.NetId);
        await OnSpawnAsync(sender, playerInfo);
        await SendObjectSpawnAsync(playerInfo);
    }

    private async ValueTask DespawnPlayerInfoAsync(InnerPlayerInfo playerInfo)
    {
        if (playerInfo.OwnerId == ServerOwned)
        {
            logger.LogDebug("Despawning PlayerInfo {nid}", playerInfo.NetId);
            GameNet.GameData.RemovePlayer(playerInfo.PlayerId);
            RemoveNetObject(playerInfo);

            await SendObjectDespawnAsync(playerInfo);
        }
    }

    private bool AddNetObject(InnerNetObject obj)
    {
        if (_allObjectsFast.ContainsKey(obj.NetId))
        {
            return false;
        }

        _allObjects.Add(obj);
        _allObjectsFast.Add(obj.NetId, obj);
        return true;
    }

    private void RemoveNetObject(InnerNetObject obj)
    {
        var index = _allObjects.IndexOf(obj);
        if (index > -1)
        {
            _allObjects.RemoveAt(index);
        }

        _allObjectsFast.Remove(obj.NetId);
    }
}
