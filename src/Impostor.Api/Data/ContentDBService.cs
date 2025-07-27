using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Api.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Api.Data;

public class ContentDBService(IHostEnvironment env, IServiceProvider service, ILogger<ContentDBService> logger, IOptions<ServerConfig> config) : IHostedService
{
    private List<IContent> Content { get; set; } = [];
    public string ContentDir { get; private set; } = Path.Combine(env.ContentRootPath, "Content");
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Content = service.GetServices<IContent>().ToList();
        ContentDir = config.Value.ContentPath.Replace("{Root}", env.ContentRootPath);
        
        if (!Directory.Exists(ContentDir))
            Directory.CreateDirectory(ContentDir);

        await LoadAllAsync(cancellationToken);
    }

    public async Task LoadAllAsync(CancellationToken? cancellationToken = null)
    {
        var files = Directory.GetFiles(ContentDir, ".content");
        foreach (var file in files)
        {
            if (!Content.TryGet(c => c.Name == Path.GetFileName(file), out var content))
                continue;
            
            await LoadAsync(content, cancellationToken);
        }
        
        logger.LogInformation("ContentDBService loaded all content count:{count}.", Content.Count);
    }

    public async Task<ContentDBService> RegisterAsync(IContent content)
    {
        if (Content.Any(c => c.Name == content.Name))
        {
            logger.LogWarning("Content with name {name} already exists.", content.Name);
            return this;
        }
        
        Content.Add(content);
        await LoadAsync(content);
        return this;
    }

    public async Task ReLoadAsync(string name)
    {
        if (!Content.TryGet(c => c.Name == name, out var content))
        {
            logger.LogWarning("Content with name {name} does not exist.", name);
            return;
        }
        
        await LoadAsync(content);
    }

    public async Task LoadAsync(IContent content, CancellationToken? token = null)
    {
        if (!content.Enable) return;
        var path = Path.Combine(ContentDir, content.Name + ".content");
        if (!File.Exists(path))
        {
            logger.LogTrace("Content with name {name} does not exist.", content.Name);
            return;
        }

        try
        {
            var text = await File.ReadAllTextAsync(path, token ?? CancellationToken.None);
            if (string.IsNullOrEmpty(text))
            {
                logger.LogTrace("Content with name {name} is empty.", content.Name);
                return;
            }
        
            await content.DeserializeAsync(text);
        }
        catch
        {
            logger.LogWarning("Content with name {name} failed to load.", content.Name);
        }
    }

    public async Task SaveAllAsync(CancellationToken? token = null)
    {
        foreach (var content in Content)
        {
            await SaveAsync(content, token);
        }
        logger.LogInformation("ContentDBService saved all content count:{count}.", Content.Count);
    }
    
    public async Task SaveAsync(string name)
    {
        if (!Content.TryGet(c => c.Name == name, out var content))
        {
            logger.LogWarning("Content with name {name} does not exist.", name);
            return;
        }
        
        await SaveAsync(content);
    }

    public async Task SaveAsync(IContent content, CancellationToken? token = null)
    {
        if (!content.Enable) return;
        var path = Path.Combine(ContentDir, content.Name + ".content");
        try
        {
            var contentText = await content.SerialiseAsync();
            await File.WriteAllTextAsync(path, await content.SerialiseAsync(), token ?? CancellationToken.None);
        }
        catch
        {
            logger.LogWarning("Content with name {name} failed to save.", content.Name);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveAllAsync(cancellationToken);
        logger.LogInformation("ContentDBService stopped.");
    }
}
