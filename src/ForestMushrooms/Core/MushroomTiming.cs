using System;

namespace ForestMushrooms.Core;

public sealed class MushroomTiming
{
    public static MushroomTiming Default { get; } = new MushroomTiming();
    public double MinimumAge { get; }
    public double RequiredWetDays { get; }
    public double InitialMeanDays { get; }
    public double RegrowthMinDays { get; }
    public double RegrowthMaxDays { get; }
    public double PickingHours { get; }
    public double DeadTreeDays { get; }
    public MushroomTiming(double minimumAge = 40, double requiredWetDays = 8,
        double initialMeanDays = 16, double regrowthMinDays = 12,
        double regrowthMaxDays = 20, double pickingHours = .5, double deadTreeDays = 2)
    {
        MinimumAge = Valid(minimumAge, 40, 0, 10000);
        RequiredWetDays = Valid(requiredWetDays, 8, 0, 10000);
        InitialMeanDays = Valid(initialMeanDays, 16, 0, 10000);
        RegrowthMinDays = Valid(regrowthMinDays, 12, 0, 10000);
        RegrowthMaxDays = Math.Max(RegrowthMinDays, Valid(regrowthMaxDays, 20, 0, 10000));
        DeadTreeDays = Valid(deadTreeDays, 2, 0, 10000);
        PickingHours = Valid(pickingHours, .5, .01, 240);
    }
    public static double Valid(double value, double fallback, double min, double max) =>
        double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
}
