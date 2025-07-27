using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Hazel;
using Hazel.Dtls;
using Il2CppSystem.Net;
using Il2CppSystem.Security.Cryptography.X509Certificates;
using InnerNet;
// ReSharper disable InconsistentNaming

namespace CertificateAdd;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
public partial class Main : BasePlugin
{
    public static List<string> Certificates { get; } = [];
    public Harmony Harmony { get; } = new(Id);

    internal static ManualLogSource? MainLog { get; private set; }
    internal static List<string> noReplace = [];
    public override void Load()
    {
        MainLog = Log;
        Harmony.PatchAll();
        
        var noReplaceServerFilePath = Path.Combine(Paths.GameRootPath, "NoReplace.json");
        if (File.Exists(noReplaceServerFilePath))
        {
            var text = File.ReadAllText(noReplaceServerFilePath);
            var content = JsonSerializer.Deserialize<List<string>>(text);
            if (content != null)
                noReplace = content;
        }
        
        var dir = Path.Combine(Paths.GameRootPath, "Certificates");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        foreach (var cer in Directory.GetFiles(dir, ".pem"))
        {
            var cert = File.ReadAllText(cer);
            Certificates.Add(cert);
        }
    }
}

[HarmonyPatch]
public static class CertificatePatches
{
    private static X509Certificate2Collection? _cachedCertificateCollection;
    private static X509Certificate2Collection GetCertificateCollection()
    {
        if (_cachedCertificateCollection != null)
            return _cachedCertificateCollection;
        
        _cachedCertificateCollection = new X509Certificate2Collection();
        var vanillaCertificate = new X509Certificate2(CryptoHelpers.DecodePEM(VanillaCertificateText));
        _cachedCertificateCollection.Add(vanillaCertificate);
        foreach (var certificate in Main.Certificates.Select(certificateText => new X509Certificate2(CryptoHelpers.DecodePEM(certificateText))))
        {
            _cachedCertificateCollection.Add(certificate);
        }
        Main.MainLog?.LogInfo($"添加验证证书: Count:{Main.Certificates.Count}");
        return _cachedCertificateCollection;
    }

    private const string VanillaCertificateText = """

                                                  -----BEGIN CERTIFICATE-----
                                                  MIIDbTCCAlWgAwIBAgIUf8xD1G/d5NK1MTjQAYGqd1AmBvcwDQYJKoZIhvcNAQEL
                                                  BQAwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUxITAfBgNVBAoM
                                                  GEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDAgFw0yMTAyMDIxNzE4MDFaGA8yMjk0
                                                  MTExODE3MTgwMVowRTELMAkGA1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUx
                                                  ITAfBgNVBAoMGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDCCASIwDQYJKoZIhvcN
                                                  AQEBBQADggEPADCCAQoCggEBAL7GFDbZdXwPYXeHWRi2GfAXkaLCgxuSADfa1pI2
                                                  vJkvgMTK1miSt3jNSg/o6VsjSOSL461nYmGCF6Ho3fMhnefOhKaaWu0VxF0GR1bd
                                                  e836YWzhWINQRwmoVD/Wx1NUjLRlTa8g/W3eE5NZFkWI70VOPRJpR9SqjNHwtPbm
                                                  Ki41PVgJIc3m/7cKOEMrMYNYoc6E9ehwLdJLQ5olJXnMoGjHo2d59hC8KW2V1dY9
                                                  sacNPUjbFZRWeQ0eJ7kbn8m3a5EuF34VEC7DFcP4NCWWI7HO5/KYE+mUNn0qxgua
                                                  r32qFnoaKZr9dXWRWJSm2XecBgqQmeF/90gdbohNNHGC/iMCAwEAAaNTMFEwHQYD
                                                  VR0OBBYEFAJAdUS5AZE3U3SPQoG06Ahq3wBbMB8GA1UdIwQYMBaAFAJAdUS5AZE3
                                                  U3SPQoG06Ahq3wBbMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEB
                                                  ALUoaAEuJf4kQ1bYVA2ax2QipkUM8PL9zoNiDjUw6ZlwMFi++XCQm8XDap45aaeZ
                                                  MnXGBqIBWElezoH6BNSbdGwci/ZhxXHG/qdHm7zfCTNaLBe2+sZkGic1x6bZPFtK
                                                  ZUjGy7LmxsXOxqGMgPhAV4JbN1+LTmOkOutfHiXKe4Z1zu09mOo9sWfGCkbIyERX
                                                  QQILBYSIkg3hU4R4xMOjvxcDrOZja6fSNyi2sgidTfe5OCKC2ovU7OmsQqzb7mFv
                                                  e+7kpIUp6AZNc49n6GWtGeOoL7JUAqMOIO+R++YQN7/dgaGDPuu0PpmgI2gPLNW1
                                                  ZwHJ755zQQRX528xg9vfykY=
                                                  -----END CERTIFICATE-----

                                                  """;

    [HarmonyPatch(typeof(DtlsUnityConnection), nameof(DtlsUnityConnection.SetValidServerCertificates)), HarmonyPrefix]
    private static void OnCreateDtlsConnectionPatch(ref X509Certificate2Collection certificateCollection)
    {
        var region = ServerManager.Instance.CurrentRegion;
        if (region.TranslateName is StringNames.ServerAS or StringNames.ServerEU or StringNames.ServerLabel or StringNames.ServerNA or StringNames.ServerSA)
            return;

        if (Main.noReplace.Contains(region.Name))
            return;
        
        certificateCollection = GetCertificateCollection();
        Main.MainLog?.LogInfo("替换证书");
    }
}
