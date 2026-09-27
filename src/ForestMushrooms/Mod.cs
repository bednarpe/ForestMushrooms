using System.Linq;
using Bindito.Core;
using HarmonyLib;
using Timberborn.EntityPanelSystem;
using Timberborn.Forestry;
using Timberborn.Gathering;
using Timberborn.Goods;
using Timberborn.ModManagerScene;
using Timberborn.TemplateInstantiation;
using Timberborn.Yielding;
using UnityEngine;

namespace ForestMushrooms;

public sealed class ModStarter : IModStarter
{
    public void StartMod(IModEnvironment environment)
    {
        new Harmony("Bedna.ForestMushrooms").PatchAll(typeof(ModStarter).Assembly);
        Debug.Log("[Forest Mushrooms] Loaded 0.1.15 for Timberborn 1.1.");
    }
}

[Context("Game")]
public sealed class MushroomConfigurator : Configurator
{
    protected override void Configure()
    {
        Bind<ForestRegistry>().AsSingleton();
        Bind<ForestMushroom>().AsTransient();
        Bind<MushroomGathering>().AsTransient();
        Bind<MushroomYielderInitializer>().AsSingleton();
        Bind<MushroomPanel>().AsSingleton();
        MultiBind<TemplateModule>().ToProvider<Templates>().AsSingleton();
        MultiBind<EntityPanelModule>().ToProvider<Panels>().AsSingleton();
    }

    public sealed class Templates : IProvider<TemplateModule>
    {
        private readonly MushroomYielderInitializer _initializer;
        public Templates(MushroomYielderInitializer initializer) { _initializer = initializer; }
        public TemplateModule Get()
        {
            var b = new TemplateModule.Builder();
            b.AddDecorator<ForestMushroomHostSpec, ForestMushroom>();
            b.AddDecorator<GathererFlag, MushroomGathering>();
            b.AddDedicatedDecorator<ForestMushroom, Yielder>(_initializer);
            return b.Build();
        }
    }

    public sealed class Panels : IProvider<EntityPanelModule>
    {
        private readonly MushroomPanel _panel;
        public Panels(MushroomPanel panel) { _panel = panel; }
        public EntityPanelModule Get()
        {
            var b = new EntityPanelModule.Builder();
            b.AddBottomFragment(_panel);
            return b.Build();
        }
    }
}

public sealed class MushroomYielderInitializer : IDedicatedDecoratorInitializer<ForestMushroom, Yielder>
{
    public void Initialize(ForestMushroom subject, Yielder decorator)
    {
        // A dedicated component, never a second GatherableSpec: that would replace sap/fruit.
        var spec = new YielderSpec
        {
            YielderComponentName = "ForestMushrooms",
            Yield = new GoodAmountSpec { Id = "Mushroom", Amount = 1 },
            RemovalTimeInHours = 0.5f,
            ResourceGroup = "Gatherable"
        };
        var strategy = decorator.GetComponentsAllocating<IRemoveYieldStrategy>().Single(s => s.Id == "Uncuttable");
        YielderCompatibility.Initialize(decorator, spec, new GoodAmount("Mushroom", 1), strategy, "Working");
        subject.Yielder = decorator;
    }
}
