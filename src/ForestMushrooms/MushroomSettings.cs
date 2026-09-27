using Bindito.Core;
using ForestMushrooms.Core;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace ForestMushrooms;

[Context("MainMenu")]
[Context("Game")]
public sealed class MushroomSettingsConfigurator : Configurator
{
    protected override void Configure() => Bind<MushroomSettings>().AsSingleton();
}

public sealed class MushroomSettings : ModSettingsOwner
{
    protected override string ModId => "Bedna.ForestMushrooms";
    public override ModSettingsContext ChangeableOn => ModSettingsContext.MainMenu | ModSettingsContext.Game;
    public override string HeaderLocKey => "ForestMushrooms.Settings.Help";
    public ModSetting<float> MinimumAge { get; } = Setting(40, "MinimumAge");
    public ModSetting<float> WetDays { get; } = Setting(8, "WetDays");
    public ModSetting<float> InitialMean { get; } = Setting(16, "InitialMean");
    public ModSetting<float> RegrowthMin { get; } = Setting(12, "RegrowthMin");
    public ModSetting<float> RegrowthMax { get; } = Setting(20, "RegrowthMax");
    public ModSetting<float> PickingHours { get; } = Setting(.5f, "PickingHours", .01f, 240);
    public ModSetting<float> DeadTreeDays { get; } = Setting(2, "DeadTreeDays");
    public MushroomTiming Current { get; private set; } = MushroomTiming.Default;

    public MushroomSettings(ISettings settings, ModSettingsOwnerRegistry registry, ModRepository repository)
        : base(settings, registry, repository) { }

    protected override void OnAfterLoad()
    {
        // Couple bounds so the displayed values always match the effective range.
        if (RegrowthMax.Value < RegrowthMin.Value) RegrowthMax.SetValue(RegrowthMin.Value);
        MinimumAge.ValueChanged += (_, _) => Refresh();
        WetDays.ValueChanged += (_, _) => Refresh();
        InitialMean.ValueChanged += (_, _) => Refresh();
        DeadTreeDays.ValueChanged += (_, _) => Refresh();
        PickingHours.ValueChanged += (_, _) => Refresh();
        RegrowthMin.ValueChanged += (_, value) => { if (RegrowthMax.Value < value) RegrowthMax.SetValue(value); Refresh(); };
        RegrowthMax.ValueChanged += (_, value) => { if (RegrowthMin.Value > value) RegrowthMin.SetValue(value); Refresh(); };
        Refresh();
    }
    private void Refresh() => Current = new MushroomTiming(MinimumAge.Value, WetDays.Value,
        InitialMean.Value, RegrowthMin.Value, RegrowthMax.Value, PickingHours.Value, DeadTreeDays.Value);
    private static ModSetting<float> Setting(float value, string key, float min = 0, float max = 10000) =>
        new BoundedSetting(value, ModSettingDescriptor.CreateLocalized("ForestMushrooms.Settings." + key)
            .SetLocalizedTooltip("ForestMushrooms.Settings." + key + ".Help"), min, max);

    private sealed class BoundedSetting : ModSetting<float>
    {
        private readonly float _min, _max;
        public BoundedSetting(float value, ModSettingDescriptor descriptor, float min, float max) : base(value, descriptor)
        { _min = min; _max = max; }
        public override void SetValue(float value) => base.SetValue((float)MushroomTiming.Valid(value, DefaultValue, _min, _max));
    }
}
