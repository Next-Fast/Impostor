using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Impostor.Api;
using Impostor.Api.Config;
using Impostor.Api.Events.Managers;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Extension.Net;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Manager;
using Impostor.Api.Net.Messages.C2S;
using Impostor.Api.Utils;
using Impostor.Server.Events.Client;
using Impostor.Server.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Next.Hazel.Dtls;
using Next.Hazel.Udp;

namespace Impostor.Server.Net.Manager;

internal sealed class NetListenerManager(
    ILogger<NetListenerManager> logger,
    ILogger<HazelConnection> connectionLogger,
    ObjectPool<MessageReader> readerPool,
    IEventManager eventManager,
    ClientManager clientManager,
    ClientAuthManager clientAuthManager,
    BanIpContent banIpContent
) : INetListenerManager
{
    public List<ListenerInfo> Listeners { get; } = [];

    public Dictionary<(string, string), X509Certificate2> CachedCertificates { get; } = new();

    public int CreateIndex { get; set; }

    public ListenerConfig? GetAvailableListener()
    {
        return GetAvailableListenerInfo()?.Config;
    }

    public event Action<INetListenerManager, ListenerConfig>? OnDisposeListener;

    public NetListenerManager CreateAll(IEnumerable<ListenerConfig> configs)
    {
        foreach (var config in configs)
        {
            Create(config);
        }

        CreateIndex = 0;
        return this;
    }

    public NetListenerManager Create(ListenerConfig config)
    {
        if (!CheckConfig(config))
        {
            logger.LogWarning("config is invalid, config: {config}", CreateIndex);
            return this;
        }

        NetworkConnectionListener listener = config.IsDtl
            ? CreateDtls(config.ListenIp, config.ListenPort + 3, ev => OnConnectionAsync(ev, config))
            : CreateUdp(config.ListenIp, config.ListenPort, ev => OnConnectionAsync(ev, config));

        var authListener = config.HasAuth
            ? CreateDtls(config.ListenIp, config.ListenPort + 2, OnAuthConnectionAsync)
            : null;

        Listeners.Add(SetCertificate(config, listener, authListener));
        CreateIndex++;
        return this;
    }

    private ListenerInfo SetCertificate(ListenerConfig config, NetworkConnectionListener? listener,
        DtlsConnectionListener? authListener)
    {
        if (!CachedCertificates.TryGetValue((config.CertificatePath, config.PrivateKeyPath), out var certificate))
        {
            if (File.Exists(config.CertificatePath) && File.Exists(config.PrivateKeyPath))
            {
                logger.LogInformation("New certificate loaded: {certificatePath} {privateKeyPath}",
                    config.CertificatePath, config.PrivateKeyPath);
                var newCertificate = DtlsHelper.GetCertificate(File.ReadAllText(config.CertificatePath),
                    File.ReadAllText(config.PrivateKeyPath));
                certificate = CachedCertificates[(config.CertificatePath, config.PrivateKeyPath)] = newCertificate;
            }
        }

        if (certificate == null)
        {
            logger.LogWarning("Certificate not found: {certificatePath} {privateKeyPath}",
                config.CertificatePath, config.PrivateKeyPath);
            
            return new ListenerInfo(config, listener, authListener);
        }

        if (listener is DtlsConnectionListener dtlsListener)
        {
            dtlsListener.SetCertificate(certificate);
        }

        authListener?.SetCertificate(certificate);

        return new ListenerInfo(config, listener, authListener);
    }

    public UdpConnectionListener CreateUdp(string ip, int port, Func<NewConnectionEventArgs, ValueTask> newConnection)
    {
        var address = IPAddress.Parse(ip.ResolveIp());
        var endpoint = new IPEndPoint(address, port);
        var mode = endpoint.AddressFamily switch
        {
            AddressFamily.InterNetwork => IPMode.IPv4,
            AddressFamily.InterNetworkV6 => IPMode.IPv6,
            _ => throw new InvalidOperationException(),
        };
        return new UdpConnectionListener(endpoint, readerPool, mode)
        {
            NewConnection = newConnection,
        };
    }

    public DtlsConnectionListener CreateDtls(string ip, int port, Func<NewConnectionEventArgs, ValueTask> newConnection)
    {
        var address = IPAddress.Parse(ip.ResolveIp());
        var endpoint = new IPEndPoint(address, port);
        var mode = endpoint.AddressFamily switch
        {
            AddressFamily.InterNetwork => IPMode.IPv4,
            AddressFamily.InterNetworkV6 => IPMode.IPv6,
            _ => throw new InvalidOperationException(),
        };
        return new DtlsConnectionListener(endpoint, readerPool, mode)
        {
            NewConnection = newConnection,
        };
    }

    private bool CheckConfig(ListenerConfig config)
    {
        if (Listeners.Any(n => n.Config.ListenIp == config.ListenIp && n.Config.ListenPort == config.ListenPort))
        {
            logger.LogWarning("Ip: {ip} Port:{port} already exists", config.ListenIp, config.ListenPort);
            return false;
        }

        if (config is { HasAuth: false, IsDtl: false })
        {
            return true;
        }

        if (config is not ({ PrivateKeyPath: "" } or { CertificatePath: "" }))
        {
            return true;
        }

        logger.LogWarning("private key or certificate path is empty not use dtl and auth");
        return false;
    }

    public async Task StartAllAsync()
    {
        foreach (var info in Listeners)
        {
            try
            {
                if (info.Listener is null)
                {
                    continue;
                }

                await info.Listener.StartAsync();
                logger.LogInformation("{name} Listener started to {ip}", info.Config.IsDtl ? "Dtl" : "Udp", info.Listener.EndPoint.ToString());

                if (info.AuthListener is null)
                {
                    continue;
                }

                await info.AuthListener.StartAsync();
                logger.LogInformation("Auth Listener started to {ip}", info.AuthListener.EndPoint.ToString());
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Failed to start listener ip:{ip} port:{port} dtl:{dtl} auth:{auth} :\n{e}",
                    info.Config.ListenIp,
                    info.Config.ListenPort,
                    info.Config.IsDtl,
                    info.Config.HasAuth,
                    e);
            }
        }
    }

    public async Task StopAllAsync()
    {
        for (var i = 0; i < Listeners.Count -1; i ++)
        {
            var info = Listeners[i];
            try
            {
                await StopListenerAsync(info);
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Failed to stop listener ip:{ip} port:{port} dtl:{dtl} auth:{auth} :\n{e}",
                    info.Config.ListenIp,
                    info.Config.ListenPort,
                    info.Config.IsDtl,
                    info.Config.HasAuth,
                    e);
            }
        }
    }

    private async ValueTask OnAuthConnectionAsync(NewConnectionEventArgs eventArgs)
    {
        if (banIpContent.CheckIsBan(eventArgs.Connection.EndPoint.Address))
        {
            logger.LogTrace("Auth Disconnect Ban Ip:{ip}", eventArgs.Connection.EndPoint.ToString());
            await eventArgs.Connection.CustomDisconnectAsync("Ip Is Banned Form Server");
            return;
        }
        
        AuthHandshakeC2S.Deserialize(eventArgs.HandshakeData, out var version, out var platform,
            out var matchmakerToken, out var friendCode);
        var info = await clientAuthManager.CreateAuthInfoAsync(version, platform, matchmakerToken, friendCode,
            eventArgs.Connection.EndPoint.Address);
        if (info == null)
        {
            await eventArgs.Connection.CustomDisconnectAsync("Auth Info Create Failed");
            logger.LogWarning("Auth Info Is Null:{ip}", eventArgs.Connection.EndPoint.ToString());
            return;
        }

        if (info.IsBanned)
        {
            await eventArgs.Connection.CustomDisconnectAsync("Banned Auth Info From Server");
            logger.LogWarning("Auth Info Is Banned:{ip}", eventArgs.Connection.EndPoint.ToString());
            return;
        }
        

        using var writer = MessageWriter.Get(MessageType.Reliable);
        writer.StartMessage(1);
        writer.Write(info.LastId);
        writer.EndMessage();
        logger.LogInformation("Has New Auth Ip:{ip} LastId:{Id}", eventArgs.Connection.EndPoint.ToString(), info.LastId);
        await eventArgs.Connection.SendAsync(writer);
    }

    private async ValueTask OnConnectionAsync(NewConnectionEventArgs eventArgs, ListenerConfig config)
    {
        if (banIpContent.CheckIsBan(eventArgs.Connection.EndPoint.Address))
        {
            logger.LogTrace("Connection Disconnect Ban Ip:{ip}", eventArgs.Connection.EndPoint.ToString());
            await eventArgs.Connection.CustomDisconnectAsync("Ip Is Banned Form Server");
            return;
        }
        
        // Handshake.
        HandshakeC2S.Deserialize(
            eventArgs.HandshakeData, config.IsDtl,
            out var clientVersion, out var name,
            out var language, out var chatMode,
            out var platformSpecificData, out var matchmakerToken,
            out var lastId, out var friendCode
        );
        
        var connection = new HazelConnection(eventArgs.Connection, connectionLogger);
        ClientAuthInfo? authInfo = null;
        if (config is { IsDtl: false, HasAuth: true })
        {
            if (lastId == 0 || !clientAuthManager.TryGetAuthInfo(lastId, out authInfo))
            {
                await connection.CustomDisconnectAsync(DisconnectReason.Custom, "Auth is required");
                logger.LogWarning("Auth Is required {ip}", eventArgs.Connection.EndPoint.ToString());
                return;
            }

            if (!authInfo.TargetIp.Equals(eventArgs.Connection.EndPoint.Address))
            {
                await connection.CustomDisconnectAsync(DisconnectReason.Custom, "Auth IP And Connection IP Not Same");
                logger.LogWarning("Auth IP And Connection IP Not Same {ip}", eventArgs.Connection.EndPoint.ToString());
                return;
            }

            if (authInfo.IsBanned)
            {
                await connection.CustomDisconnectAsync(DisconnectReason.Custom, "Banned Auth Info From Server");
                logger.LogWarning("Auth Info Is Banned:{ip}", eventArgs.Connection.EndPoint.ToString());
                return;
            }
        }
        
        logger.LogInformation(
            "Has New Connection Ip:{ip} isDtl:{dtl} Name:{name} LastId:{Id} FriendCode:{code} MatchmakerToken:{token}",
            eventArgs.Connection.EndPoint.ToString(),
            config.IsDtl,
            name,
            lastId,
            friendCode ?? authInfo?.FriendCode ?? string.Empty,
            matchmakerToken ?? authInfo?.MatchmakerToken ?? string.Empty);

        await eventManager.CallAsync(new ClientConnectionEvent(connection, eventArgs.HandshakeData));

        // Register client
        await clientManager.RegisterConnectionAsync(connection, name, clientVersion, language, chatMode,
            platformSpecificData, authInfo);
    }

    public ListenerInfo? GetAvailableListenerInfo()
    {
        return Listeners.FirstOrDefault();
    }

    public async Task StopListenerAsync(ListenerInfo info)
    {
        if (info.Listener is not null)
        {
            await info.Listener.DisposeAsync();
        }

        if (info.AuthListener is not null)
        {
            await info.AuthListener.DisposeAsync();
        }
        Listeners.Remove(info);
        OnDisposeListener?.Invoke(this, info.Config);
    }

    public record ListenerInfo(
        ListenerConfig Config,
        NetworkConnectionListener? Listener,
        DtlsConnectionListener? AuthListener);
}
