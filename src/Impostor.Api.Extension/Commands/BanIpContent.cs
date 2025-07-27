using System.Net;
using Impostor.Api.Data;
using Impostor.Api.Extension.Utils;
using Microsoft.Extensions.Logging;

namespace Impostor.Api.Extension.Commands;

public class BanIpContent(ILogger<BanIpContent> logger) : IContent
{
    public string Name => "BanIp";
    public bool Enable => true;
    
    public class BanIpInfo(IPAddress ip, DateTime startTime, DateTime endTime)
    {
        public IPAddress IP { get; set; } = ip;
        public DateTime StartTime { get; set; } = startTime;
        public DateTime EndTime { get; set; } = endTime;
    }
    
    
    public List<BanIpInfo> _banIps = [];
    public Task<string> SerialiseAsync()
    {
        return Task.FromResult(JsonUtils.Serialize(_banIps));
    }

    public Task BanIpAsync(IPAddress address, TimeSpan span)
    {
        if (_banIps.Any(ban => ban.IP.Equals(address)))
        {
            logger.LogWarning("Ip {0} 已被 Ban", address);
            return Task.CompletedTask;
        }

        var start = DateTime.Now;
        var end = start.Add(span);
        _banIps.Add(new BanIpInfo(address, start, end));
        logger.LogInformation("Ban Ip {ip} {span} 从 {start} 到 {end}", address, span, start, end);
        return Task.CompletedTask;
    }
    
    public bool IsBan(string ip) => CheckIsBan(IPAddress.Parse(ip));
    
    public void UnBan(string ip) => UnBan(IPAddress.Parse(ip));

    public void UnBan(IPAddress ip)
    {
        var info = _banIps.FirstOrDefault(ban => ban.IP.Equals(ip));
        if (info == null)
        {
            return;
        }
        
        _banIps.Remove(info);
        logger.LogInformation("UnBan Ip {ip}", ip);
    }

    public bool CheckIsBan(IPAddress ip)
    {
        var info = _banIps.FirstOrDefault(ban => ban.IP.Equals(ip));
        if (info == null)
        {
            return false;
        }

        if (info.EndTime < DateTime.Now)
        {
            _banIps.Remove(info);
            return false;
        }
        
        return true;
    }

    public Task DeserializeAsync(string data)
    {
        var json = JsonUtils.Deserialize<List<BanIpInfo>>(data);
        if (json == null)
        {
            return Task.CompletedTask;
        }
        
        _banIps = json;
        return Task.CompletedTask;
    }
}
