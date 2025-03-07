﻿using System;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Impostor.Server.Net.Factories;

internal class ClientFactory<TClient>(IServiceProvider serviceProvider) : IClientFactory
    where TClient : ClientBase
{
    public ClientBase Create(IHazelConnection connection, string name, GameVersion clientVersion, Language language,
        QuickChatModes chatMode, PlatformSpecificData platformSpecificData)
    {
        var client = ActivatorUtilities.CreateInstance<TClient>(serviceProvider, name, clientVersion, language,
            chatMode, platformSpecificData, connection);
        connection.Client = client;
        return client;
    }
}
