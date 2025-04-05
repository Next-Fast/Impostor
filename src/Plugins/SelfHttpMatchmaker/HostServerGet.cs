using System.Net;
using System.Text.Json.Serialization;
using Impostor.Api.Config;
using Impostor.Api.Net.Manager;
using Microsoft.Extensions.Logging;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker;

public class HostServerGet(INetListenerManager listenerManager, Logger<HostServerGet> logger) : IHostServer
{
    private bool _hasAdd;
    
    private ListenerConfig? _currentConfig;

    [JsonPropertyName("Ip")]
    public long Ip
    {
        get
        {
            Check();
            
            if (_currentConfig == null)
            {
                logger.LogError("CurrentConfig is null");
                return 0;
            }

            var address = IPAddress.Parse(_currentConfig.PublicIp);
#pragma warning disable CS0618 // 类型或成员已过时
            return address.Address;
#pragma warning restore CS0618 // 类型或成员已过时
        }
    }

    [JsonPropertyName("Port")]
    public ushort Port
    {
        get
        {
            Check();
            
            if (_currentConfig != null)
            {
                return _currentConfig.PublicPort;
            }

            logger.LogError("CurrentConfig is null");
            return 0;
        }
    }

    private void Check()
    {
        if (!_hasAdd)
        {
            listenerManager.OnDisposeListener += OnDisposeListener; 
            _hasAdd = true;    
        }

        if (_currentConfig != null)
            return;
        
        _currentConfig = listenerManager.GetAvailableListener();
    }

    private void OnDisposeListener(INetListenerManager manager, ListenerConfig config)
    {
        if (_currentConfig != config)
            return;
        
        _currentConfig = null;
    }
}
