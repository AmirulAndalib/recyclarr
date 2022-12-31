using Autofac;
using CliFx.Attributes;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Recyclarr.Cli.Config;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Http;
using Recyclarr.TrashLib.Services.CustomFormat;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Radarr;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Serilog;

namespace Recyclarr.Cli.Command;

[Command("radarr", Description = "Perform operations on a Radarr instance")]
[UsedImplicitly]
internal class RadarrCommand : ServiceCommand
{
    // ReSharper disable MemberCanBePrivate.Global

    [CommandOption("list-custom-formats", Description =
        "List available custom formats from the guide in YAML format.")]
    public bool ListCustomFormats { get; [UsedImplicitly] set; }

    [CommandOption("list-qualities", Description =
        "List available quality definition types from the guide.")]
    public bool ListQualities { get; [UsedImplicitly] set; }

    // ReSharper restore MemberCanBePrivate.Global

    public override string Name => "Radarr";

    public override async Task Process(ILifetimeScope container)
    {
        await base.Process(container);

        var lister = container.Resolve<IRadarrGuideDataLister>();
        var log = container.Resolve<ILogger>();
        var guideService = container.Resolve<IRadarrGuideService>();
        var console = container.Resolve<IConsole>();

        if (ListCustomFormats)
        {
            lister.ListCustomFormats();
            return;
        }

        if (ListQualities)
        {
            lister.ListQualities();
            return;
        }

        var configFinder = container.Resolve<IConfigurationFinder>();
        var configLoader = container.Resolve<IConfigurationLoader<RadarrConfiguration>>();
        foreach (var config in configLoader.LoadMany(configFinder.GetConfigFiles(Config), "radarr"))
        {
            await using var scope = container.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(config).As<IServiceConfiguration>();
            });

            var serverName = Name;
            var instanceName = config.Name ?? FlurlLogging.SanitizeUrl(config.BaseUrl);

            await console.Output.WriteLineAsync($@"
===========================================
Processing {serverName} Server: [{instanceName}]
===========================================
");

            log.Debug("Processing {Server} server {Name}", serverName, instanceName);

            var validator = scope.Resolve<ConfigValidationExecutor>();
            if (!validator.Validate(config))
            {
                log.Error("Due to validation failure, this instance will be skipped");
                continue;
            }

            // ReSharper disable InvertIf

            if (config.QualityDefinition != null)
            {
                var updater = scope.Resolve<IQualitySizeUpdater>();
                await updater.Process(Preview, config.QualityDefinition, guideService);
            }

            if (config.CustomFormats.Count > 0)
            {
                var updater = scope.Resolve<ICustomFormatUpdater>();
                await updater.Process(Preview, config.CustomFormats, guideService);
            }

            // ReSharper restore InvertIf
        }
    }
}
