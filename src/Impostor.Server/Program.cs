using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Impostor.Api.Config;
using Impostor.Api.Events.Managers;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Extension.Messages;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Net.Manager;
using Impostor.Api.Utils;
using Impostor.Server.Commands;
using Impostor.Server.Events;
using Impostor.Server.Events.Player;
using Impostor.Server.Net;
using Impostor.Server.Net.Factories;
using Impostor.Server.Net.Manager;
using Impostor.Server.Plugins;
using Impostor.Server.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Next.Hazel.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Settings.Configuration;

namespace Impostor.Server;

internal static class Program
{
    private static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Impostor v{0}", DotnetUtils.Version);
            CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Impostor terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string? GetArg(this string[] args, string name)
    {
        if (!args.Contains(name))
        {
            return null;
        }

        var index = Array.IndexOf(args, name);
        return index + 1 < args.Length ? args[index + 1] : null;
    }

    private static IConfiguration CreateBaseConfiguration(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.SetBasePath(args.GetArg("--base") ?? Directory.GetCurrentDirectory());
        configurationBuilder.AddJsonFile(args.GetArg("--config") ?? "config.json", true);
        configurationBuilder.AddEnvironmentVariables(args.GetArg("--prefix") ?? "IMPOSTOR_");
        configurationBuilder.AddCommandLine(args);

        return configurationBuilder.Build();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var configuration = CreateBaseConfiguration(args)
            .GetConfig<ServerConfig>(ServerConfig.Section, out var serverConfig)
            .GetConfig<PluginConfig>(PluginConfig.Section, out var pluginConfig);

        var hostBuilder = Host.CreateDefaultBuilder(args)
            .LoadPlugins(pluginConfig)
            .ConfigureConfiguration(configuration)
            .ConfigureService(serverConfig)
            .ConfigurePluginService(pluginConfig)
            .ConfigureLog(serverConfig)
            .UseContentRoot(serverConfig.RootPath ?? Directory.GetCurrentDirectory())
            .UseEnvironment(serverConfig.Env ?? DotnetUtils.Environment)
            .UseConsoleLifetime();

        return hostBuilder;
    }

    private static IHostBuilder ConfigureConfiguration(this IHostBuilder builder, IConfiguration baseConfiguration)
    {
        var pluginBuilder = new ConfigurationBuilder();

        foreach (var plugin in PluginLoader.AllPluginLoad)
        {
            plugin.Startup?.ConfigureConfiguration(pluginBuilder);
        }

        return builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddConfiguration(baseConfiguration);
            configurationBuilder.AddConfiguration(pluginBuilder.Build());
        });
    }

    private static IHostBuilder ConfigureService(this IHostBuilder builder,
        ServerConfig config)
    {
        builder
            .ConfigureServices((host, services) =>
            {
                services
                    .AddHazel()
                    .AddPolicy<PlayerMovementEvent.PlayerMovementEventObjectPolicy, PlayerMovementEvent>();

                services
                    .ConfigureSection<AntiCheatConfig>(host.Configuration, AntiCheatConfig.Section)
                    .ConfigureSection<CompatibilityConfig>(host.Configuration, CompatibilityConfig.Section)
                    .ConfigureSection<ServerConfig>(host.Configuration, ServerConfig.Section)
                    .ConfigureSection<TimeoutConfig>(host.Configuration, TimeoutConfig.Section)
                    .ConfigureSection<PluginConfig>(host.Configuration, PluginConfig.Section);

                services
                    .AddSingleton<ClientAuthManager>()
                    .AddSingleton<IMessageWriterProvider, MessageWriterProvider>()
                    .AddSingleton<IGameCodeFactory, GameCodeFactory>()
                    .AddSingleton<IEventManager, EventManager>()
                    .AddSingleton<IDateTimeProvider, RealDateTimeProvider>()
                    .AddSingleton<ICompatibilityManager, CompatibilityManager>()
                    .AddSingleton<IClientFactory, ClientFactory<Client>>();

                services
                    .AddRequiredSingleton<IClientManager, ClientManager>()
                    .AddRequiredSingleton<IGameManager, GameManager>()
                    .AddRequiredSingleton<ICommandManager, CommandManager>()
                    .AddRequiredSingleton<INetListenerManager, NetListenerManager>();

                if (config.EnableCommands)
                {
                    services.AddHostedService<ConsoleCommandService>();
                }

                services.AddHostedService<StarterService>();
            });
        return builder;
    }

    private static IHostBuilder ConfigureLog(this IHostBuilder hostBuilder, ServerConfig serverConfig)
    {
        hostBuilder.UseSerilog((context, loggerConfiguration) =>
        {
            AssemblyLoadContext.Default.Resolving += LoadSerilogAssembly;

            loggerConfiguration
                .MinimumLevel.Is(serverConfig.LogLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .LoggerSet(serverConfig)
                .ReadFrom.Configuration(context.Configuration,
                    new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles));

            AssemblyLoadContext.Default.Resolving -= LoadSerilogAssembly;
        });
        return hostBuilder;

        Assembly? LoadSerilogAssembly(AssemblyLoadContext loadContext, AssemblyName name)
        {
            var paths = new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (var path in paths)
            {
                try
                {
                    return loadContext.LoadFromAssemblyPath(Path.Combine(path, name.Name + ".dll"));
                }
                catch (FileNotFoundException)
                {
                }
            }

            return null;
        }
    }

    private static LoggerConfiguration LoggerSet(this LoggerConfiguration config, ServerConfig serverConfig)
    {
        return serverConfig switch
        {
            { WriteConsole: true, WriteFile: true } => config.WriteTo.Console().WriteTo.File(serverConfig.LogFilePath),
            { WriteConsole: false, WriteFile: true } => config.WriteTo.File(serverConfig.LogFilePath),
            { WriteConsole: true, WriteFile: false } => config.WriteTo.Console(),
            _ => config,
        };
    }

    private static IConfiguration GetConfig<T>(this IConfiguration configuration, string section, out T result)
        where T : class, new()
    {
        result = configuration.GetSection(section)
            .Get<T>() ?? new T();
        return configuration;
    }
}
