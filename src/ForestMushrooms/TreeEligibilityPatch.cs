using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ForestMushrooms.Core;
using HarmonyLib;
using Timberborn.BlueprintSystem;
using Timberborn.Forestry;
using Timberborn.TemplateSystem;
using Timberborn.NaturalResourcesMoisture;

namespace ForestMushrooms;

public sealed record ForestMushroomHostSpec : ComponentSpec;

// Filter before decoration, including planting previews. Ineligible plants never
// receive ForestMushroom or its extra Yielder and never enter the forest registry.
[HarmonyPatch(typeof(Blueprint), MethodType.Constructor,
    new[] { typeof(string), typeof(IEnumerable<ComponentSpec>), typeof(ImmutableArray<Blueprint>) })]
public static class TreeEligibilityPatch
{
    public static void Prefix(ref IEnumerable<ComponentSpec> specs)
    {
        if (specs == null) return;
        var list = specs.ToList();
        bool eligible = TreeEligibility.IsHost(list.OfType<TreeComponentSpec>().Any(),
            list.OfType<TemplateSpec>().FirstOrDefault()?.TemplateName,
            list.OfType<FloodableNaturalResourceSpec>().Any(s => s.MinWaterHeight > 0));
        list.RemoveAll(s => s is ForestMushroomHostSpec);
        if (eligible) list.Add(new ForestMushroomHostSpec());
        specs = list;
    }
}
