using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.IO;

namespace RandomRoundEvents;

internal sealed class ClownGrenades
{
    private sealed class ProxyBinding
    {
        public required CBaseCSGrenadeProjectile Projectile { get; init; }
        public required CDynamicProp Proxy { get; init; }
    }

    private static readonly IReadOnlyList<string> ModelKeywords = new List<string>
    {
        "bottle",
        "wine",
        "fruit",
        "food",
        "melon",
        "banana",
        "orange",
        "lemon",
        "pepper",
        "onion",
        "potato",
        "zucchini",
        "bowl",
        "cup",
        "mug",
        "plate",
        "ceramic",
        "paint",
        "cone",
        "traffic",
        "crate",
        "bucket",
        "barrel",
        "drum",
        "toolbox",
        "trash",
        "paint",
        "garbage"
    }.AsReadOnly();

    private static readonly IReadOnlyList<string> BlockedModelKeywords = new List<string>
    {
        "garbagebin",
        "trashcan",
        "trash_",
        "wheel",
        "lid",
        "glass",
        "window",
        "piece",
        "shard",
        "fragment",
        "frame",
        "hinge",
        "support",
        "holder",
        "cap_",
        "cover",
        "door",
        "sign",
        "mailboxkit01_letter",
        "postcard",
        "calendar",
        "barricade",
        "concrete_bag",
        "cement_bag",
        "stack_",
        "/test/",
        "_test_",
        "test_animation",
        "animation",
        "vertigo_tools",
        "screwdriver",
        "hammer",
        "wrench",
        "pliers"
    }.AsReadOnly();

    private static readonly IReadOnlyList<string> AllowedModelPathKeywords = new List<string>
    {
        "models/props/",
        "models/de_",
        "models/cs_",
        "models/generic/",
        "models/food/",
        "/junk/",
        "/construction/",
        "/garbage/",
        "/decorations/"
    }.AsReadOnly();

    private readonly RandomRoundEvents _plugin;
    private readonly Random _random = new();
    private readonly List<CDynamicProp> _spawnedProps = [];
    private readonly Dictionary<nint, ProxyBinding> _trackedProjectiles = [];
    private readonly List<string> _cachedPropModels = [];
    private CounterStrikeSharp.API.Modules.Timers.Timer? _followTimer;
    private bool _spawnListenerRegistered;

    public ClownGrenades(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply()
    {
        _plugin.ShowEvent("Clown Grenades Round", "Grenades keep their normal behavior, but fly as junk props instead.");
        _plugin.GiveAllPlayersStandardLoadout();

        if (!_spawnListenerRegistered)
        {
            _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _spawnListenerRegistered = true;
        }

        EnsureFollowTimer();
    }

    public void Reset()
    {
        _followTimer?.Kill();
        _followTimer = null;

        foreach (var binding in _trackedProjectiles.Values)
        {
            RestoreProjectileVisibility(binding.Projectile);

            if (binding.Proxy.IsValid)
                binding.Proxy.Remove();
        }

        foreach (var prop in _spawnedProps)
        {
            if (prop.IsValid)
                prop.Remove();
        }

        _trackedProjectiles.Clear();
        _spawnedProps.Clear();
        _cachedPropModels.Clear();

        if (_spawnListenerRegistered)
        {
            _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _spawnListenerRegistered = false;
        }
    }

    public void OnEntitySpawned(CEntityInstance entity)
    {
        if (!_plugin.IsClownGrenadesRoundActive || !IsSupportedGrenadeProjectile(entity.DesignerName))
            return;

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (!grenade.IsValid)
                return;

            AttachVisualProxy(grenade);
        });
    }

    private void AttachVisualProxy(CBaseCSGrenadeProjectile grenade)
    {
        string? modelName = NormalizeModelPath(GetPropModel(grenade));
        if (string.IsNullOrWhiteSpace(modelName))
        {
            _plugin.LogClownGrenadesWarning("[RandomRoundEvents] No usable stock prop model was found for Clown Grenades Round.");
            return;
        }

        try
        {
            var proxy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (proxy == null || !proxy.IsValid)
                return;

            proxy.Spawnflags = 256u;
            proxy.RenderMode = RenderMode_t.kRenderNormal;
            proxy.Render = Color.FromArgb(255, 255, 255, 255);
            var kv = new CEntityKeyValues();
            kv.SetString("model", modelName);
            kv.SetFloat("modelscale", _plugin.Config.ClownGrenadesModelScale);
            proxy.DispatchSpawn(kv);
            proxy.Teleport(grenade.AbsOrigin, grenade.AbsRotation, grenade.AbsVelocity);

            HideProjectile(grenade);

            _spawnedProps.Add(proxy);
            _trackedProjectiles[grenade.Handle] = new ProxyBinding
            {
                Projectile = grenade,
                Proxy = proxy
            };

            EnsureFollowTimer();

            if (_plugin.Config.Debug)
                _plugin.LogClownGrenadesInfo("[RandomRoundEvents] Clown Grenades using model {Model}", modelName);
        }
        catch (Exception ex)
        {
            _plugin.LogClownGrenadesWarning("[RandomRoundEvents] Failed to attach Clown Grenades proxy model {Model}: {Error}", modelName, ex.Message);
        }
    }

    private void EnsureFollowTimer()
    {
        if (_followTimer != null)
            return;

        _followTimer = _plugin.AddTimer(0.02f, UpdateTrackedProjectiles, TimerFlags.REPEAT);
    }

    private void UpdateTrackedProjectiles()
    {
        if (_trackedProjectiles.Count == 0)
            return;

        foreach (var handle in _trackedProjectiles.Keys.ToList())
        {
            if (!_trackedProjectiles.TryGetValue(handle, out var binding))
                continue;

            if (!binding.Projectile.IsValid)
            {
                if (binding.Proxy.IsValid)
                    binding.Proxy.Remove();

                _trackedProjectiles.Remove(handle);
                continue;
            }

            if (!binding.Proxy.IsValid)
            {
                _trackedProjectiles.Remove(handle);
                RestoreProjectileVisibility(binding.Projectile);
                continue;
            }

            HideProjectile(binding.Projectile);
            binding.Proxy.Teleport(binding.Projectile.AbsOrigin, binding.Projectile.AbsRotation, binding.Projectile.AbsVelocity);
        }
    }

    private string? GetPropModel(CBaseCSGrenadeProjectile grenade)
    {
        if (_cachedPropModels.Count == 0)
            CachePropModels();

        if (_cachedPropModels.Count > 0)
            return _cachedPropModels[_random.Next(_cachedPropModels.Count)];

        return null;
    }

    private void CachePropModels()
    {
        var preferred = new List<string>();
        var fallback = new List<string>();
        var mapFallback = new List<string>();
        var sampledCandidates = new List<string>();

        foreach (var instance in Utilities.GetAllEntities())
        {
            if (instance == null || !instance.IsValid)
                continue;

            var designerName = instance.DesignerName ?? string.Empty;
            if (designerName.Contains("weapon", StringComparison.OrdinalIgnoreCase) ||
                designerName.Contains("grenade", StringComparison.OrdinalIgnoreCase) ||
                designerName.Contains("player", StringComparison.OrdinalIgnoreCase) ||
                designerName.Contains("chicken", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var modelName = GetEntityModelName(instance);
            if (string.IsNullOrWhiteSpace(modelName))
                continue;

            if (_plugin.Config.Debug && sampledCandidates.Count < 20)
                sampledCandidates.Add($"{designerName} => {modelName}");

            if (designerName.Equals("prop_physics_multiplayer", StringComparison.OrdinalIgnoreCase))
            {
                // Dust2 often exposes almost no fun dynamic props at all, but it
                // does tend to expose the soccer ball through prop_physics_multiplayer.
                // Keep this as a narrow fallback instead of reopening the broader
                // physics-model path that caused earlier instability.
                if (modelName.Contains("soccer", StringComparison.OrdinalIgnoreCase) ||
                    modelName.Contains("ball", StringComparison.OrdinalIgnoreCase))
                {
                    mapFallback.Add(modelName);
                }

                continue;
            }

            if (!LooksLikeUsablePropModel(modelName))
                continue;

            if (BlockedModelKeywords.Any(keyword => modelName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (preferred.Contains(modelName, StringComparer.OrdinalIgnoreCase) ||
                fallback.Contains(modelName, StringComparer.OrdinalIgnoreCase) ||
                mapFallback.Contains(modelName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (designerName.Equals("prop_dynamic", StringComparison.OrdinalIgnoreCase))
            {
                if (ModelKeywords.Any(keyword => modelName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    preferred.Add(modelName);
                else
                    fallback.Add(modelName);
                continue;
            }

            if (modelName.Contains("ball", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("soccer", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("skybox", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("grenade", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("weapons/models/", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("weapon_", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("agent", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("character", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("player", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("/glass_", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("_glass_", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("window", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ModelKeywords.Any(keyword => modelName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                preferred.Add(modelName);
            else if (modelName.Contains("models/", StringComparison.OrdinalIgnoreCase))
                fallback.Add(modelName);
        }

        _cachedPropModels.Clear();
        if (preferred.Count > 0)
            _cachedPropModels.AddRange(preferred);
        else if (fallback.Count > 0)
            _cachedPropModels.AddRange(fallback);
        else
            _cachedPropModels.AddRange(mapFallback);

        if (_plugin.Config.Debug)
        {
            _plugin.LogClownGrenadesInfo(
                "[RandomRoundEvents] Clown Grenades scan found {PreferredCount} preferred, {FallbackCount} fallback, and {MapFallbackCount} map fallback models.",
                preferred.Count,
                fallback.Count,
                mapFallback.Count);

            if (_cachedPropModels.Count == 0 && sampledCandidates.Count > 0)
            {
                _plugin.LogClownGrenadesInfo(
                    "[RandomRoundEvents] Clown Grenades sampled models: {Samples}",
                    string.Join(" | ", sampledCandidates));
            }
        }
    }

    private static bool LooksLikeUsablePropModel(string modelName)
    {
        if (!AllowedModelPathKeywords.Any(keyword => modelName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (modelName.Contains("weapons/models/", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("weapon_", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("player", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("agent", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("character", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("chicken", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("hostage", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("ball", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("soccer", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("skybox", StringComparison.OrdinalIgnoreCase) ||
            modelName.Contains("grenade", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string? GetEntityModelName(CEntityInstance instance)
    {
        return Models.TryGetLoadedModelName(instance);
    }

    private static string? NormalizeModelPath(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;

        if (Path.HasExtension(modelName))
            return modelName;

        return $"{modelName}.vmdl";
    }

    private static void HideProjectile(CBaseCSGrenadeProjectile grenade)
    {
        if (!grenade.IsValid)
            return;

        grenade.RenderMode = RenderMode_t.kRenderNone;
        grenade.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(grenade, "CBaseModelEntity", "m_nRenderMode");
        Utilities.SetStateChanged(grenade, "CBaseModelEntity", "m_clrRender");
    }

    private static void RestoreProjectileVisibility(CBaseCSGrenadeProjectile grenade)
    {
        if (!grenade.IsValid)
            return;

        grenade.RenderMode = RenderMode_t.kRenderNormal;
        grenade.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(grenade, "CBaseModelEntity", "m_nRenderMode");
        Utilities.SetStateChanged(grenade, "CBaseModelEntity", "m_clrRender");
    }

    private static bool IsSupportedGrenadeProjectile(string? designerName)
    {
        return designerName == "flashbang_projectile" ||
               designerName == "smokegrenade_projectile" ||
               designerName == "hegrenade_projectile" ||
               designerName == "decoy_projectile" ||
               designerName == "molotov_projectile" ||
               designerName == "incgrenade_projectile";
    }
}
