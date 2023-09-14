using YamlDotNet.Core;

namespace Recyclarr.TrashLib.Config.Parsing.ErrorHandling;

public static class ConfigContextualMessages
{
    public static string? GetContextualErrorFromException(YamlException e)
    {
        return LookupMessage(e.Message) ?? LookupMessage(e.InnerException?.Message);
    }

    private static string? LookupMessage(string? msg)
    {
        if (msg is null)
        {
            return null;
        }

        if (msg.Contains(
            "Property 'reset_unmatched_scores' not found on type " +
            $"'{typeof(QualityScoreConfigYaml).FullName}'"))
        {
            return
                "Usage of 'reset_unmatched_scores' inside 'quality_profiles' under 'custom_formats' is no " +
                "longer supported. Use the root-level 'quality_profiles' instead. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v5.0/#reset-unmatched-scores";
        }

        if (msg.Contains(
            $"Invalid cast from 'System.String' to '{typeof(ResetUnmatchedScoresConfigYaml).FullName}'"))
        {
            return
                "Using true/false with `reset_unmatched_scores` is no longer supported. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v6.0/#reset-scores";
        }

        return null;
    }
}
