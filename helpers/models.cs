using CounterStrikeSharp.API.Core;

namespace RandomRoundEvents;

internal static class Models
{
    internal static string? TryGetLoadedModelName(CEntityInstance instance)
    {
        if (!instance.IsValid)
            return null;

        try
        {
            var entity = instance.As<CBaseEntity>();
            if (!entity.IsValid)
                return null;

            return entity.As<CBaseModelEntity>().CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName;
        }
        catch
        {
            return null;
        }
    }
}
