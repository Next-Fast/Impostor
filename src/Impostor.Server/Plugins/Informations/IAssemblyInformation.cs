﻿using System.Reflection;
using System.Runtime.Loader;

namespace Impostor.Server.Plugins.Informations;

public interface IAssemblyInformation
{
    AssemblyName AssemblyName { get; }

    bool IsPlugin { get; }

    Assembly Load(AssemblyLoadContext context);
}
