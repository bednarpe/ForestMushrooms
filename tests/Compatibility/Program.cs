using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Immutable;
using System.Collections.Generic;
using ForestMushrooms;
using HarmonyLib;
using Timberborn.BlueprintSystem;
using Timberborn.Forestry;
using Timberborn.TemplateSystem;
using ModSettings.Core;
using Timberborn.Goods;
using Timberborn.Yielding;
using System.Linq;
using Timberborn.FactionSystem;
using Timberborn.Workshops;
using Timberborn.NaturalResourcesMoisture;

internal static class Program
{
    public static void Main(string[] args)
    {
        var game = Path.GetFullPath(args[0]);
        var harmony = Path.GetFullPath(args[1]);
        var settings = Path.GetFullPath(args[2]);
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            var path = name.Name == "0Harmony" ? harmony : Path.Combine(
                name.Name.StartsWith("ModSettings.") ? settings : game, name.Name + ".dll");
            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
        };
        Run();
        CheckSettings();
        CheckYielder();
        CheckFactions();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        // Exercise the production prefix and actual game constructor without
        // Unity. The game's Mono-specific Harmony build cannot detour .NET 8.
        if (typeof(Blueprint).GetConstructor(new[] { typeof(string), typeof(IEnumerable<ComponentSpec>), typeof(ImmutableArray<Blueprint>) }) == null)
            throw new Exception("Blueprint constructor signature changed");
            foreach (var name in new[] { "Succulent", "Succulent.LeafCoats", "Mangrove", "Mangrove.NewFaction", "CustomWaterTree", "Pine", "Oak" })
            {
                IEnumerable<ComponentSpec> specs = new ComponentSpec[] {
                    new TemplateSpec { TemplateName = name }, new TreeComponentSpec(),
                    new FloodableNaturalResourceSpec { MinWaterHeight = name == "CustomWaterTree" ? 1 : 0 }
                };
                TreeEligibilityPatch.Prefix(ref specs);
                var blueprint = new Blueprint(name, specs, ImmutableArray<Blueprint>.Empty);
                bool expected = !name.StartsWith("Succulent") && !name.StartsWith("Mangrove") && name != "CustomWaterTree";
                if (blueprint.HasSpec<ForestMushroomHostSpec>() != expected)
                    throw new Exception("Wrong mushroom decoration for " + name);
                if (!blueprint.HasSpec<TreeComponentSpec>() || blueprint.GetSpec<TemplateSpec>().TemplateName != name)
                    throw new Exception("Original plant specs changed: " + name);
                Console.WriteLine("PASS actual blueprint decoration: " + name);
            }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckYielder()
    {
        var target = new Yielder(null);
        var spec = new YielderSpec { YielderComponentName = "ForestMushrooms",
            Yield = new GoodAmountSpec { Id = "Mushroom", Amount = 1 }, RemovalTimeInHours = .5f, ResourceGroup = "Gatherable" };
        YielderCompatibility.Initialize(target, spec, new GoodAmount("Mushroom", 1), null, "Working");
        if (target.YielderSpec != spec || target.Yield.GoodId != "Mushroom" || target.Yield.Amount != 1
            || target.ComponentName != "ForestMushrooms" || target.Animation != "Working")
            throw new Exception("Actual game Yielder initialization failed");
        var persistence = typeof(Yielder).GetField("_persistYield", BindingFlags.Instance | BindingFlags.NonPublic);
        if (persistence != null && !(bool)persistence.GetValue(target))
            throw new Exception("Yield persistence must remain enabled");
        Console.WriteLine("PASS actual Yielder initialization and persistence compatibility");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFactions()
    {
        foreach (var name in new[] { "Folktails", "IronTeeth", "LeafCoats", "PreviouslyUnknownFaction" })
        {
            var faction = new FactionSpec { Id = name,
                GoodCollectionIds = ImmutableArray.Create("OriginalGoods"),
                NeedCollectionIds = ImmutableArray.Create("OriginalNeeds") };
            IEnumerable<ComponentSpec> specs = new[] { faction };
            FactionCompatibilityPatch.Prefix(ref specs);
            FactionCompatibilityPatch.Prefix(ref specs);
            var blueprint = new Blueprint(name, specs, ImmutableArray<Blueprint>.Empty);
            var updated = blueprint.GetSpec<FactionSpec>();
            if (!updated.GoodCollectionIds.SequenceEqual(new[] { "OriginalGoods", FactionCompatibilityPatch.CollectionId })
                || !updated.NeedCollectionIds.SequenceEqual(new[] { "OriginalNeeds", FactionCompatibilityPatch.CollectionId })
                || faction.GoodCollectionIds.Length != 1 || updated.Id != name)
                throw new Exception("Faction collections not preserved/idempotent: " + name);
        }
        foreach (var scenario in new[] {
            ("Fermenter.Unknown", "CustomFood", true),
            ("ForestFermenter.Folktails", "FermentedMushroom", true),
            ("Fermenter.LeafCoats", "FermentedChestnut", true),
            ("FoodProcessor.Unknown", "FermentedCustomFruit", true),
            ("Gristmill.Unknown", "Flour", false),
            ("FermenterStorage.Unknown", "CustomFood", false) })
        {
            var original = new ManufactorySpec { ProductionRecipeIds = ImmutableArray.Create(scenario.Item2) };
            IEnumerable<ComponentSpec> specs = new ComponentSpec[] {
                new TemplateSpec { TemplateName = scenario.Item1 }, original };
            FactionCompatibilityPatch.Prefix(ref specs);
            TreeEligibilityPatch.Prefix(ref specs);
            FactionCompatibilityPatch.Prefix(ref specs);
            var blueprint = new Blueprint(scenario.Item1, specs, ImmutableArray<Blueprint>.Empty);
            var recipes = blueprint.GetSpec<ManufactorySpec>().ProductionRecipeIds;
            if (recipes.Contains("FermentedMushroom") != scenario.Item3 || recipes[0] != scenario.Item2
                || recipes.Count(id => id == "FermentedMushroom") > 1 || original.ProductionRecipeIds.Length != 1)
                throw new Exception("Wrong recipe compatibility: " + scenario.Item1);
        }
        IEnumerable<ComponentSpec> defaults = new ComponentSpec[] {
            new FactionSpec { Id = "Empty" }, new TemplateSpec { TemplateName = "Fermenter.Empty" }, new ManufactorySpec() };
        FactionCompatibilityPatch.Prefix(ref defaults);
        var empty = new Blueprint("Empty", defaults, ImmutableArray<Blueprint>.Empty);
        if (empty.GetSpec<FactionSpec>().NeedCollectionIds.Length != 1
            || empty.GetSpec<ManufactorySpec>().ProductionRecipeIds.Single() != "FermentedMushroom")
            throw new Exception("Default arrays are not supported");
        Console.WriteLine("PASS generic factions/fermenters: unknown faction, preserved recipes, duplicates, non-fermenters and empty arrays");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckSettings()
    {
        // Exercise actual ModSetting value events without registry/disk access.
        var owner = new MushroomSettings(null, null, null);
        int count = 0;
        foreach (var property in typeof(MushroomSettings).GetProperties())
            if (typeof(ModSetting<float>).IsAssignableFrom(property.PropertyType))
            { var setting = (ModSetting<float>)property.GetValue(owner); setting.SetValue(setting.DefaultValue); count++; }
        typeof(MushroomSettings).GetMethod("OnAfterLoad", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        if (count != 7 || owner.Current.MinimumAge != 40 || owner.Current.RequiredWetDays != 8
            || owner.Current.InitialMeanDays != 16 || owner.Current.RegrowthMinDays != 12
            || owner.Current.RegrowthMaxDays != 20 || owner.Current.DeadTreeDays != 2 || owner.Current.PickingHours != .5)
            throw new Exception("Incorrect default UI settings");
        owner.MinimumAge.SetValue(7);
        owner.WetDays.SetValue(2);
        owner.InitialMean.SetValue(3);
        owner.PickingHours.SetValue(2);
        owner.DeadTreeDays.SetValue(9);
        if (owner.Current.MinimumAge != 7 || owner.Current.RequiredWetDays != 2 || owner.Current.InitialMeanDays != 3 || owner.Current.PickingHours != 2 || owner.Current.DeadTreeDays != 9)
            throw new Exception("Settings do not update current timing");
        owner.RegrowthMin.SetValue(30);
        if (owner.RegrowthMax.Value != 30 || owner.Current.RegrowthMaxDays != 30) throw new Exception("Regrowth upper bound not repaired");
        owner.RegrowthMax.SetValue(4);
        if (owner.RegrowthMin.Value != 4 || owner.Current.RegrowthMinDays != 4) throw new Exception("Regrowth lower bound not repaired");
        owner.PickingHours.SetValue(float.NaN);
        owner.MinimumAge.SetValue(-9);
        if (owner.PickingHours.Value != .5 || owner.MinimumAge.Value != 0) throw new Exception("Invalid input not sanitized");
        Console.WriteLine("PASS seven settings: defaults, live events, range bounds and invalid values");
    }
}
