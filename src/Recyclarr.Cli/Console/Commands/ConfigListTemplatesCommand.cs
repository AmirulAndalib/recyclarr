using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.TrashLib.Config.Parsing.ErrorHandling;
using Recyclarr.TrashLib.Repo;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

[UsedImplicitly]
[Description("List local configuration files.")]
public class ConfigListTemplatesCommand : AsyncCommand<ConfigListTemplatesCommand.CliSettings>
{
    private readonly ILogger _log;
    private readonly ConfigListTemplateProcessor _processor;
    private readonly IMultiRepoUpdater _repoUpdater;

    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    public class CliSettings : BaseCommandSettings
    {
    }

    public ConfigListTemplatesCommand(ILogger log, ConfigListTemplateProcessor processor, IMultiRepoUpdater repoUpdater)
    {
        _log = log;
        _processor = processor;
        _repoUpdater = repoUpdater;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CliSettings settings)
    {
        try
        {
            await _repoUpdater.UpdateAllRepositories(settings.CancellationToken);
            _processor.Process();
            return 0;
        }
        catch (NoConfigurationFilesException)
        {
            _log.Error("No configuration files found");
        }

        return 1;
    }
}
