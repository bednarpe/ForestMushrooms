using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HarmonyLib;
using Timberborn.BlueprintSystem;
using Timberborn.FactionSystem;
using Timberborn.TemplateSystem;
using Timberborn.Workshops;

namespace ForestMushrooms;

// Runs after blueprint JSON/modifiers are resolved and before templates are decorated.
[HarmonyPatch(typeof(Blueprint), MethodType.Constructor,
    new[] { typeof(string), typeof(IEnumerable<ComponentSpec>), typeof(ImmutableArray<Blueprint>) })]
public static class FactionCompatibilityPatch
{
    public const string CollectionId = "Bedna.ForestMushrooms";
    public const string RecipeId = "FermentedMushroom";

    public static void Prefix(ref IEnumerable<ComponentSpec> specs)
    {
        if (specs == null) return;
        var list = specs.ToList();
        var templateName = list.OfType<TemplateSpec>().FirstOrDefault()?.TemplateName;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is FactionSpec faction)
                list[i] = faction with {
                    GoodCollectionIds = Append(faction.GoodCollectionIds, CollectionId),
                    NeedCollectionIds = Append(faction.NeedCollectionIds, CollectionId)
                };
            else if (list[i] is ManufactorySpec factory && IsFermenter(templateName, factory.ProductionRecipeIds))
                list[i] = factory with { ProductionRecipeIds = Append(factory.ProductionRecipeIds, RecipeId) };
        }
        specs = list;
    }

    private static bool IsFermenter(string templateName, ImmutableArray<string> recipes)
    {
        // Use internal template/recipe IDs, never translated display labels.
        var buildingName = templateName?.Split('.')[0];
        return buildingName?.EndsWith("Fermenter", StringComparison.OrdinalIgnoreCase) == true
            || (!recipes.IsDefaultOrEmpty && recipes.Any(id =>
                id?.StartsWith("Fermented", StringComparison.OrdinalIgnoreCase) == true));
    }

    private static ImmutableArray<string> Append(ImmutableArray<string> values, string value) =>
        values.IsDefault ? ImmutableArray.Create(value) : values.Contains(value) ? values : values.Add(value);
}
