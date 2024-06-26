using System.Collections.ObjectModel;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.PipelinePhases;

public class QualitySizeTransactionPhase(ILogger log) : ITransactionPipelinePhase<QualitySizePipelineContext>
{
    public void Execute(QualitySizePipelineContext context)
    {
        // Do not check ConfigOutput for null since the LogPhase does it for us
        var guideQuality = context.ConfigOutput!.Qualities;
        var serverQuality = context.ApiFetchOutput;

        var newQuality = new Collection<ServiceQualityDefinitionItem>();
        foreach (var qualityData in guideQuality)
        {
            var serverEntry = serverQuality.FirstOrDefault(q => q.Quality?.Name == qualityData.Quality);
            if (serverEntry == null)
            {
                log.Warning("Server lacks quality definition for {Quality}; it will be skipped", qualityData.Quality);
                continue;
            }

            if (!QualityIsDifferent(serverEntry, qualityData))
            {
                continue;
            }

            // Not using the original list again, so it's OK to modify the definition ref type objects in-place.
            serverEntry.MinSize = qualityData.MinForApi;
            serverEntry.MaxSize = qualityData.MaxForApi;
            serverEntry.PreferredSize = qualityData.PreferredForApi;
            newQuality.Add(serverEntry);

            log.Debug("Setting Quality " +
                "[Name: {Name}] [Source: {Source}] [Min: {Min}] [Max: {Max}] [Preferred: {Preferred}]",
                serverEntry.Quality?.Name, serverEntry.Quality?.Source, serverEntry.MinSize, serverEntry.MaxSize,
                serverEntry.PreferredSize);
        }

        context.TransactionOutput = newQuality;
    }

    private static bool QualityIsDifferent(ServiceQualityDefinitionItem a, QualitySizeItem b)
    {
        return b.IsMinDifferent(a.MinSize) || b.IsMaxDifferent(a.MaxSize) ||
            a.PreferredSize is not null && b.IsPreferredDifferent(a.PreferredSize);
    }
}
