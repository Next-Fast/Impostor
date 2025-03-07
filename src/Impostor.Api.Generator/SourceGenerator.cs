using System;
using System.Diagnostics.CodeAnalysis;
using CSharpPoet;
using Impostor.Api.Generator.Generators;
using Microsoft.CodeAnalysis;

namespace Impostor.Api.Generator;

[Generator(LanguageNames.CSharp)]
public sealed class SourceGenerator : IIncrementalGenerator
{
    private const string DataPath = "Innersloth/Data/";
    private const string LanguagePath = "Languages/";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select((analyzerConfigOptions, _) =>
            {
                if (!analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir",
                        out var projectDirectory))
                {
                    throw new Exception("Couldn't get project directory");
                }

                return new Options(projectDirectory.NormalizePath());
            });

        var filesProvider = context.AdditionalTextsProvider.Combine(optionsProvider)
            .Where(static pair =>
            {
                var (file, options) = pair;
                return options.TryGetRelativePath(file.Path, out var relativePath) && relativePath.EndsWith(".json");
            })
            .Select(static (pair, cancellationToken) =>
            {
                var (file, options) = pair;

                if (!options.TryGetRelativePath(file.Path, out var relativePath))
                {
                    throw new InvalidOperationException();
                }

                return (
                    RelativePath: relativePath,
                    Content: file.GetText(cancellationToken)!.ToString()
                );
            })
            .Collect();

        context.RegisterSourceOutput(filesProvider, (spc, files) =>
        {
            if (files.IsEmpty)
            {
                throw new InvalidOperationException($"No json files found in Impostor.Api/{DataPath}");
            }

            var generator = new BaseGenerator(spc, files);
            
            var enumGenerator = generator.GetEnum();
            enumGenerator.Generate("ColorType", "Impostor.Api.Innersloth.Customization");
            enumGenerator.Generate("DisconnectReason", sourceName: "DisconnectReasons");
            enumGenerator.Generate("GameKeywords", flags: true, underlyingType: CSharpEnumUnderlyingType.UnsignedInt);
            enumGenerator.Generate("GameOverReason", underlyingType: CSharpEnumUnderlyingType.Byte);
            enumGenerator.Generate("Platforms");
            enumGenerator.Generate("RoleTypes", underlyingType: CSharpEnumUnderlyingType.UnsignedShort);
            enumGenerator.Generate("RulesPresets", underlyingType: CSharpEnumUnderlyingType.Byte);
            enumGenerator.Generate("SpecialGameModes", underlyingType: CSharpEnumUnderlyingType.Byte);
            enumGenerator.Generate("StringNames");
            enumGenerator.Generate("SystemTypes", underlyingType: CSharpEnumUnderlyingType.Byte);
            enumGenerator.Generate("Language", sourceName: "SupportedLangs");
            enumGenerator.Generate("TaskTypes");
            enumGenerator.Generate("RpcCalls", "Impostor.Api.Net.Inner", underlyingType: CSharpEnumUnderlyingType.Byte);

            var mapDataGenerator = generator.GetMapData();
            var mapNames = new[] { "Skeld", "Mira", "April", "Polus", "Airship", "Fungle" };
            foreach (var mapName in mapNames)
            {
                mapDataGenerator.Generate(mapName);
            }
            
            /*var languageGenerator = generator.GetLanguage();
            languageGenerator.Generate("English");*/
        });
    }

    private readonly record struct Options(string ProjectDirectory)
    {
        public bool TryGetRelativePath(string path, [NotNullWhen(true)] out string? relativePath)
        {
            if (path.NormalizePath().TryTrimStart(ProjectDirectory, out var projectPath))
            {
                if (projectPath.TryTrimStart(DataPath, out relativePath))
                {
                    return true;
                }

                if (projectPath.TryTrimStart(LanguagePath, out relativePath))
                {
                    return true;
                }
            }

            relativePath = null;
            return false;
        }
    }
}
