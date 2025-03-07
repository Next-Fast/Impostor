﻿using System.Threading.Tasks;
using Impostor.Api.Extension.Events;

namespace Impostor.Api.Games;

public interface IGameCodeFactory
{
    ValueTask<GameCode> CreateAsync(GameCreationEvent? creationEvent);
    ValueTask<GameCode> Create();
}
