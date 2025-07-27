using Impostor.Api.Config;
using Impostor.Api.Data;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Impostor.Api.Extension.Utils;

public static class ServiceUtils
{
    public static IServiceCollection ConfigureSection<T>(this IServiceCollection collection,
        IConfiguration configuration, string section) where T : class
    {
        return collection.Configure<T>(configuration.GetSection(section));
    }

    public static IServiceCollection AddBanIpContent(this IServiceCollection services, ServerConfig config)
    {
        if (config.EnableContent)
        {
            services.AddRequiredSingleton<IContent, BanIpContent>();
            return services;
        }

        services.AddSingleton<BanIpContent>();
        return services;
    }
    
    public class ServiceCacheGet<T>(IServiceProvider provider) where T : class
    {
        private T? _value;
        public T? Value => Get();
        
        public IServiceProvider Provider { get; private set; } = provider;

        public ServiceCacheGet<T> SetProvider(IServiceProvider provider)
        {
            Provider = provider;
            _noHasValue = false;
            return this;
        }
        
        public Func<ServiceCacheGet<T>, bool>? HasUpdate { get; set; }

        public Func<IServiceProvider, ServiceCacheGet<T>, T>? OnUpdate { get; set; }

        public bool _noHasValue;

        private T? Get()
        {
            if (HasUpdate?.Invoke(this) ?? false)
            {
                _value = OnUpdate != null ? OnUpdate.Invoke(Provider, this) : Provider.GetService<T>();

                return _value;
            }

            if (_noHasValue)
            {
                return null;
            }

            if (_value != null)
            {
                return _value;
            }

            var get = Provider.GetService<T>();
            _noHasValue = get == null;
            return _value = get;
        }

        public static implicit operator T?(ServiceCacheGet<T> serviceCacheGet) => serviceCacheGet.Get();
    }
}
