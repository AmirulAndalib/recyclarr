using Autofac.Core;
using Flurl.Http;
using Recyclarr.Cli.Console;
using Recyclarr.Common.FluentValidation;
using Recyclarr.Compatibility;
using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.VersionControl;
using YamlDotNet.Core;

namespace Recyclarr.Cli.Processors.ErrorHandling;

internal class ConsoleExceptionHandler(ILogger log)
{
    public async Task<bool> HandleException(Exception sourceException)
    {
        switch (sourceException)
        {
            case GitCmdException e:
                log.Error(
                    e,
                    "Non-zero exit code {ExitCode} while executing Git command",
                    e.ExitCode
                );
                break;

            case FlurlHttpException e:
                log.Error(e, "HTTP error");
                var httpExceptionHandler = new FlurlHttpExceptionHandler(log);
                await httpExceptionHandler.ProcessServiceErrorMessages(
                    new ServiceErrorMessageExtractor(e)
                );
                break;

            case NoConfigurationFilesException:
                log.Error("No configuration files found");
                break;

            case InvalidInstancesException e:
                log.Error("The following instances do not exist: {Names}", e.InstanceNames);
                break;

            case DuplicateInstancesException e:
                log.Error("The following instance names are duplicated: {Names}", e.InstanceNames);
                log.Error("Instance names are unique and may not be reused");
                break;

            case SplitInstancesException e:
                log.Error(
                    "The following configs share the same `base_url`, which isn't allowed: {Instances}",
                    e.InstanceNames
                );
                log.Error(
                    "Consolidate the config files manually to fix. "
                        + "See: <https://recyclarr.dev/wiki/yaml/config-examples/#merge-single-instance>"
                );
                break;

            case InvalidConfigurationFilesException e:
                log.Error(
                    "Manually-specified configuration files do not exist: {Files}",
                    e.InvalidFiles
                );
                break;

            case InvalidConfigurationException:
                log.Error("One or more invalid configurations were found");
                break;

            case PostProcessingException e:
                log.Error(e, "Configuration post-processing failed");
                break;

            case ServiceIncompatibilityException e:
                log.Error("{Message}", e.Message);
                break;

            case CommandException e:
                log.Error("{Message}", e.Message);
                break;

            case YamlException e:
                log.Error(e, "Exception while parsing settings.yml at line {Line}", e.Start.Line);
                if (e.Data["ContextualMessage"] is string context)
                {
                    log.Error("{Context}", context);
                }

                break;

            case ContextualValidationException e:
                e.LogErrors(new ValidationLogger(log));
                break;

            case DependencyResolutionException { InnerException: not null } e:
                return await HandleException(e.InnerException!);

            default:
                return false;
        }

        return true;
    }
}
