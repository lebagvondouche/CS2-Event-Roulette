using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace RandomRoundEvents;

internal sealed class Diagnostics
{
    private readonly RandomRoundEvents _plugin;

    public Diagnostics(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    internal void DumpLoadedModels(CCSPlayerController? player)
    {
        int scanned = 0;
        int validEntities = 0;
        int entitiesWithModels = 0;
        var modelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var modelSamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var designerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var instance in Utilities.GetAllEntities())
        {
            scanned++;

            if (instance == null || !instance.IsValid)
                continue;

            validEntities++;

            var designerName = string.IsNullOrWhiteSpace(instance.DesignerName) ? "<null>" : instance.DesignerName!;
            if (designerCounts.TryGetValue(designerName, out int designerCount))
                designerCounts[designerName] = designerCount + 1;
            else
                designerCounts[designerName] = 1;

            var modelName = Models.TryGetLoadedModelName(instance);
            if (string.IsNullOrWhiteSpace(modelName))
                continue;

            entitiesWithModels++;

            if (modelCounts.TryGetValue(modelName, out int modelCount))
                modelCounts[modelName] = modelCount + 1;
            else
                modelCounts[modelName] = 1;

            if (!modelSamples.ContainsKey(modelName))
                modelSamples[modelName] = designerName;
        }

        _plugin.Logger.LogInformation(
            "[RandomRoundEvents] DumpModels: scanned={Scanned} validEntities={ValidEntities} readableModels={ReadableModels} uniqueModels={UniqueModels} uniqueDesigners={UniqueDesigners}",
            scanned,
            validEntities,
            entitiesWithModels,
            modelCounts.Count,
            designerCounts.Count);

        foreach (var model in modelCounts
                     .OrderByDescending(pair => pair.Value)
                     .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                     .Take(50))
        {
            var sampleDesigner = modelSamples.TryGetValue(model.Key, out var designer) ? designer : "<unknown>";
            _plugin.Logger.LogInformation(
                "[RandomRoundEvents] DumpModels model count={Count} sampleDesigner={Designer} model={Model}",
                model.Value,
                sampleDesigner,
                model.Key);
        }

        foreach (var designer in designerCounts
                     .OrderByDescending(pair => pair.Value)
                     .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                     .Take(40))
        {
            _plugin.Logger.LogInformation(
                "[RandomRoundEvents] DumpModels designer count={Count} designer={Designer}",
                designer.Value,
                designer.Key);
        }

        player?.PrintToChat($" {ChatColors.Blue}[EVENT]{ChatColors.White} Dumped server-side model info to the plugin log.");
    }
}
