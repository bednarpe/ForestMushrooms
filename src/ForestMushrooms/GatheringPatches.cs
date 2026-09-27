using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Timberborn.BaseComponentSystem;
using Timberborn.BehaviorSystem;
using Timberborn.BuildingsNavigation;
using Timberborn.Gathering;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.TemplateSystem;
using Timberborn.WorkSystem;
using Timberborn.Yielding;

namespace ForestMushrooms;

public sealed class MushroomGathering : BaseComponent, IAwakableComponent
{
    private readonly ForestRegistry _forest;
    private readonly IGoodService _goods;
    private BuildingTerrainRange _range;
    private Workplace _workplace;
    public MushroomGathering(ForestRegistry forest, IGoodService goods) { _forest = forest; _goods = goods; }
    public static bool IsGatheringBuilding(BaseComponent building) => building.HasComponent<GathererFlag>()
        && building.GetComponent<YieldRemovingBuildingSpec>()?.ResourceGroup == "Gatherable"
        && building.GetComponent<MushroomGathering>()?._goods.HasGood("Mushroom") == true;
    public void Awake()
    {
        _range = GetComponent<BuildingTerrainRange>();
        _workplace = GetComponent<Workplace>();
    }
    public bool AddYielders(IList<Yielder> result, string templateName)
    {
        if (!IsGatheringBuilding(this) || _range == null || _workplace.NumberOfAssignedWorkers == 0) return false;
        foreach (var worker in _workplace.AssignedWorkers)
            if (worker.WorkerType != "Beaver") return false;
        bool found = false;
        foreach (var cell in _range.GetRange())
        {
            if (!_forest.TryGet(cell, out var tree)) continue;
            if (templateName != null && !tree.GetComponent<TemplateSpec>().IsNamed(templateName)) continue;
            bool ready = tree.Available;
            if (!ready && !tree.Growing) continue;
            // GetYielders' return value reports resources in range, including
            // crops still growing. Only ripe crops may become worker targets.
            found = true;
            if (ready && !tree.Yielder.Reservable.Reserved && !result.Contains(tree.Yielder)) result.Add(tree.Yielder);
        }
        return found;
    }
}

[HarmonyPatch(typeof(InRangeYielders), nameof(InRangeYielders.GetYielders))]
public static class GatherMushroomsPatch
{
    public static void Postfix(InRangeYielders __instance, IList<Yielder> yielders, string templateName, ref bool __result)
    {
        var gathering = __instance.GetComponent<MushroomGathering>();
        if (gathering != null) __result |= gathering.AddYielders(yielders, templateName);
    }
}

[HarmonyPatch(typeof(InRangeYielders), nameof(InRangeYielders.GetYields))]
public static class MushroomInventoryPatch
{
    public static void Postfix(InRangeYielders __instance, HashSet<string> yields)
    {
        // Keep this output allowed even between crops and when the forest changes.
        if (MushroomGathering.IsGatheringBuilding(__instance)) yields.Add("Mushroom");
    }
}

[HarmonyPatch(typeof(YieldRemovingBuilding), nameof(YieldRemovingBuilding.GetAllowedGoods))]
public static class MushroomAllowedGoodsPatch
{
    public static void Postfix(YieldRemovingBuilding __instance, ref IEnumerable<string> __result)
    {
        if (MushroomGathering.IsGatheringBuilding(__instance))
            __result = __result.Concat(new[] { "Mushroom" }).Distinct();
    }
}

public static class HarvestGuard
{
    public static bool Invalid(YielderRemover remover)
    {
        var yielder = remover.ReservedYielder;
        if (yielder == null || yielder.ComponentName != "ForestMushrooms") return false;
        var tree = yielder.GetComponent<ForestMushroom>();
        var worker = remover.GetComponent<Worker>();
        return tree == null || !tree.Available || !yielder.IsYielding || worker == null
            || worker.WorkerType != "Beaver" || !worker.Employed
            || !worker.Workplace.HasComponent<GathererFlag>()
            || !worker.Workplace.GetComponent<YieldRemovingBuilding>().IsAllowed(yielder.YielderSpec);
    }
    public static void Cancel(YielderRemover remover)
    {
        remover.GetComponent<GoodReserver>()?.UnreserveCapacity();
        remover.Unreserve();
    }
}

[HarmonyPatch(typeof(YieldRemoverBehavior), nameof(YieldRemoverBehavior.Decide))]
public static class MushroomWalkGuardPatch
{
    public static bool Prefix(YieldRemoverBehavior __instance, ref Decision __result)
    {
        var remover = __instance.GetComponent<YielderRemover>();
        if (!HarvestGuard.Invalid(remover)) return true;
        HarvestGuard.Cancel(remover);
        __result = Decision.ReleaseNextTick();
        return false;
    }
}

[HarmonyPatch(typeof(YielderRemover), nameof(YielderRemover.CompleteReservation))]
public static class MushroomCompletionGuardPatch
{
    public static bool Prefix(YielderRemover __instance)
    {
        // Drying or chopping can happen while the beaver is working at the tree.
        if (!HarvestGuard.Invalid(__instance)) return true;
        HarvestGuard.Cancel(__instance);
        return false;
    }
}
