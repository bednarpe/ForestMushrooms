using System;
using ForestMushrooms.Core;
using Newtonsoft.Json;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using Timberborn.Growing;
using Timberborn.LevelVisibilitySystem;
using Timberborn.NaturalResourcesLifecycle;
using Timberborn.Persistence;
using Timberborn.SoilMoistureSystem;
using Timberborn.SoilContaminationSystem;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;
using Timberborn.WorldPersistence;
using Timberborn.Yielding;
using UnityEngine;

namespace ForestMushrooms;

public sealed class ForestMushroom : TickableComponent, IAwakableComponent,
    IPostInitializableEntity, IDeletableEntity, IPersistentEntity
{
    private static readonly ComponentKey SaveKey = new ComponentKey("Bedna.ForestMushrooms");
    private static readonly PropertyKey<string> StateKey = new PropertyKey<string>("StateV1");
    private readonly IDayNightCycle _clock;
    private readonly ForestRegistry _forest;
    private readonly IGoodService _goods;
    private readonly MushroomSettings _settings;
    private readonly ISoilContaminationService _soilContamination;
    private readonly ILevelVisibilityService _levelVisibility;
    private ContaminatedObject _contaminatedObject;
    public MushroomTiming Timing => _settings.Current;
    private LivingNaturalResource _living;
    private Growable _growable;
    private DryObject _dry;
    private Cuttable _cuttable;
    private BlockObject _block;
    private bool _initialized;
    private MushroomVisual _visual;
    public MushroomState State { get; private set; }
    public Yielder Yielder { get; set; }
    public Vector3Int Coordinates => _block.Coordinates;
    public bool IsCut => _cuttable != null && _cuttable.Yielder.IsYieldRemoved;
    public bool Alive => _living != null && !_living.IsDead && !IsCut;
    public bool Grown => _growable != null && _growable.IsGrown;
    public bool Wet => _dry != null && !_dry.IsDry;
    public bool Contaminated => _block != null && !_block.IsPreview && _soilContamination.SoilIsContaminated(Coordinates);
    public int Neighbors => _forest.Neighbors(Coordinates);
    public bool Growing => _initialized && GoodsAvailable && !State.HasMushroom
        && Alive && Grown && Wet && !Contaminated && Neighbors == TreeEligibility.RequiredNeighbors;
    public bool Available => _initialized && GoodsAvailable && State.HasMushroom && !IsCut && !Contaminated
        && (Alive ? Grown && Wet && Neighbors == TreeEligibility.RequiredNeighbors
            : _living != null && _living.IsDead && State.DeadDays < Timing.DeadTreeDays);
    public bool GoodsAvailable => _goods.HasGood("Mushroom");

    public ForestMushroom(IDayNightCycle clock, ForestRegistry forest, IGoodService goods, MushroomSettings settings,
        ISoilContaminationService soilContamination, ILevelVisibilityService levelVisibility)
    {
        _clock = clock; _forest = forest; _goods = goods; _settings = settings;
        _soilContamination = soilContamination; _levelVisibility = levelVisibility;
    }

    public void Awake()
    {
        _living = GetComponent<LivingNaturalResource>();
        _growable = GetComponent<Growable>();
        _dry = GetComponent<DryObject>();
        _cuttable = GetComponent<Cuttable>();
        _block = GetComponent<BlockObject>();
        _contaminatedObject = GetComponent<ContaminatedObject>();
        State = MushroomState.Create(unchecked((uint)Guid.NewGuid().GetHashCode()), Timing);
        if (_dry != null) _dry.EnteredDryState += OnDry;
        if (_living != null) _living.Died += OnDead;
        if (_cuttable != null) _cuttable.WasCut += OnCut;
        if (_contaminatedObject != null) _contaminatedObject.EnteredContaminatedState += OnContaminated;
        Yielder.YieldDecreased += OnPicked;
    }

    public void PostInitializeEntity()
    {
        if (_block.IsPreview) { DisableComponent(); return; }
        _initialized = true;
        _levelVisibility.MaxVisibleLevelChanged += OnMaxVisibleLevelChanged;
        _forest.Add(this);
        Synchronize();
    }

    public override void Tick()
    {
        if (!_initialized) return;
        State.Advance(_clock.FixedDeltaTimeInHours / 24.0, Alive, Grown, Wet, Neighbors == TreeEligibility.RequiredNeighbors, Timing, Contaminated);
        Synchronize();
    }

    internal void Synchronize()
    {
        State.ApplyContamination(Contaminated, Timing);
        bool ready = Available && GoodsAvailable;
        if (ready && !Yielder.IsYielding) Yielder.ResetYield();
        else if (!ready && Yielder.Enabled) { Yielder.RemoveRemainingYield(); Yielder.Disable(); }
        if (ready && _visual == null) _visual = new MushroomVisual(Transform, Coordinates, _block.BaseZ);
        UpdateVisualVisibility();
    }

    // Level changes can happen while paused. Only rendering changes: hidden
    // crops remain available to the simulation and retain their growth state.
    private void OnMaxVisibleLevelChanged(object sender, int level) => UpdateVisualVisibility();
    private void UpdateVisualVisibility() =>
        _visual?.SetVisible(Available && _levelVisibility.BlockIsVisible(Coordinates));

    private void OnDry(object sender, EventArgs e)
    {
        State.Advance(0, Alive, Grown, false, Neighbors == TreeEligibility.RequiredNeighbors, Timing, Contaminated);
        Synchronize();
    }
    private void OnDead(object sender, EventArgs e)
    {
        State.Advance(0, false, Grown, Wet, false, Timing, Contaminated);
        Synchronize();
    }
    private void OnCut(object sender, EventArgs e) { State.Cut(Timing); Synchronize(); }
    private void OnContaminated(object sender, EventArgs e) { Synchronize(); }
    private void OnPicked(object sender, EventArgs e) { State.Harvest(Timing); Synchronize(); }

    public bool DebugMature()
    {
        if (!Alive || !Grown || !Wet || Contaminated || Neighbors != TreeEligibility.RequiredNeighbors || !GoodsAvailable) return false;
        State.ForceMature(Timing);
        Synchronize();
        return true;
    }

    public void DebugPick() { if (Available && State.Harvest(Timing)) Synchronize(); }

    public void DeleteEntity()
    {
        _initialized = false;
        _levelVisibility.MaxVisibleLevelChanged -= OnMaxVisibleLevelChanged;
        _forest.Remove(this);
        if (_dry != null) _dry.EnteredDryState -= OnDry;
        if (_living != null) _living.Died -= OnDead;
        if (_cuttable != null) _cuttable.WasCut -= OnCut;
        if (_contaminatedObject != null) _contaminatedObject.EnteredContaminatedState -= OnContaminated;
        Yielder.YieldDecreased -= OnPicked;
        _visual?.Dispose();
    }

    public void Save(IEntitySaver saver) => saver.GetComponent(SaveKey).Set(StateKey, JsonConvert.SerializeObject(State));
    public void Load(IEntityLoader loader)
    {
        if (loader.TryGetComponent(SaveKey, out var component) && component.Has(StateKey))
            State = JsonConvert.DeserializeObject<MushroomState>(component.Get(StateKey))
                ?? throw new InvalidOperationException("Forest Mushrooms: null saved tree state.");
    }
}
