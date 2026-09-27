using System;
using Timberborn.Goods;
using Timberborn.Yielding;

namespace ForestMushrooms;

// Timberborn added persistYield between the spec and good amount in September
// 2026. Resolve once so this DLL also continues to work with earlier 1.1 builds.
public static class YielderCompatibility
{
    private delegate void CurrentInitialize(Yielder target, YielderSpec spec, bool persistYield,
        GoodAmount yield, IRemoveYieldStrategy strategy, string animation);
    private delegate void LegacyInitialize(Yielder target, YielderSpec spec,
        GoodAmount yield, IRemoveYieldStrategy strategy, string animation);
    private static readonly Action<Yielder, YielderSpec, GoodAmount, IRemoveYieldStrategy, string> InitializeYield = Resolve();

    public static void Initialize(Yielder target, YielderSpec spec, GoodAmount yield,
        IRemoveYieldStrategy strategy, string animation) => InitializeYield(target, spec, yield, strategy, animation);

    private static Action<Yielder, YielderSpec, GoodAmount, IRemoveYieldStrategy, string> Resolve()
    {
        var current = typeof(Yielder).GetMethod("Initialize", new[] {
            typeof(YielderSpec), typeof(bool), typeof(GoodAmount), typeof(IRemoveYieldStrategy), typeof(string)
        });
        if (current != null)
        {
            var initialize = (CurrentInitialize)Delegate.CreateDelegate(typeof(CurrentInitialize), current);
            // Preserve both ripe and empty yields across save/load, as the old API did.
            return (target, spec, yield, strategy, animation) => initialize(target, spec, true, yield, strategy, animation);
        }
        var legacy = typeof(Yielder).GetMethod("Initialize", new[] {
            typeof(YielderSpec), typeof(GoodAmount), typeof(IRemoveYieldStrategy), typeof(string)
        });
        if (legacy != null)
        {
            var initialize = (LegacyInitialize)Delegate.CreateDelegate(typeof(LegacyInitialize), legacy);
            return (target, spec, yield, strategy, animation) => initialize(target, spec, yield, strategy, animation);
        }
        throw new MissingMethodException("Forest Mushrooms: unsupported Timberborn Yielder.Initialize API. Update the mod for this game version.");
    }
}
