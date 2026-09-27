using System;

namespace ForestMushrooms.Core;

// Only simulation days enter this class. All random state is saved with the tree.
public sealed class MushroomState
{


    public double AgeDays { get; set; }
    public double WetDays { get; set; }
    public double RemainingDays { get; set; }
    public bool HasMushroom { get; set; }
    public uint RandomState { get; set; }
    public int HarvestCount { get; set; }
    public double DeadDays { get; set; }
    public bool WasContaminated { get; set; }

    public static MushroomState Create(uint seed, MushroomTiming timing = null)
    {
        timing ??= MushroomTiming.Default;
        var state = new MushroomState { RandomState = seed == 0 ? 1u : seed };
        state.RemainingDays = -timing.InitialMeanDays * Math.Log(1 - state.Next());
        return state;
    }

    private double Next()
    {
        uint x = RandomState == 0 ? 1u : RandomState;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        RandomState = x;
        return (x + 0.5) / 4294967296.0;
    }

    public void Advance(double days, bool alive, bool grown, bool wet, bool interior, MushroomTiming timing = null, bool contaminated = false)
    {
        timing ??= MushroomTiming.Default;
        if (double.IsNaN(days) || double.IsInfinity(days) || days < 0)
            throw new ArgumentOutOfRangeException(nameof(days));
        ApplyContamination(contaminated, timing);
        if (!alive)
        {
            WetDays = 0;
            DeadDays += days;
            if (DeadDays >= timing.DeadTreeDays) HasMushroom = false;
            return; // Dead trees retain only existing crops; they never grow new ones.
        }
        DeadDays = 0;
        if (contaminated) { AgeDays += days; return; }
        double oldAge = AgeDays;
        double oldWet = WetDays;
        AgeDays += days;
        WetDays = wet ? WetDays + days : 0;
        if (!wet || !interior || !grown)
        {
            if (HasMushroom) { HasMushroom = false; ScheduleRegrowth(timing); }
            return;
        }
        if (HasMushroom) return;
        // Count only the portion of a tick after BOTH thresholds were crossed.
        double blocked = Math.Max(0, Math.Max(timing.MinimumAge - oldAge, timing.RequiredWetDays - oldWet));
        RemainingDays = Math.Max(0, RemainingDays - Math.Max(0, days - blocked));
        if (AgeDays >= timing.MinimumAge && WetDays >= timing.RequiredWetDays && RemainingDays <= 0)
            HasMushroom = true;
    }

    public bool Harvest(MushroomTiming timing = null)
    {
        if (!HasMushroom || WasContaminated) return false;
        HasMushroom = false;
        HarvestCount++;
        ScheduleRegrowth(timing);
        return true;
    }

    public void Invalidate()
    {
        HasMushroom = false;
        WetDays = 0;
    }

    public void ApplyContamination(bool contaminated, MushroomTiming timing = null)
    {
        if (contaminated)
        {
            Invalidate();
            // Restart the initial growth wait once per contamination episode,
            // including when no ripe crop existed. Keep the tree's actual age.
            if (!WasContaminated)
                RemainingDays = -(timing ?? MushroomTiming.Default).InitialMeanDays * Math.Log(1 - Next());
        }
        WasContaminated = contaminated;
    }

    public void Cut(MushroomTiming timing = null)
    {
        Invalidate();
        AgeDays = 0;
        DeadDays = 0;
        RemainingDays = -(timing ?? MushroomTiming.Default).InitialMeanDays * Math.Log(1 - Next());
    }

    public void ForceMature(MushroomTiming timing = null)
    {
        AgeDays = Math.Max(AgeDays, (timing ?? MushroomTiming.Default).MinimumAge);
        WetDays = Math.Max(WetDays, (timing ?? MushroomTiming.Default).RequiredWetDays);
        RemainingDays = 0;
        HasMushroom = true;
    }

    private void ScheduleRegrowth(MushroomTiming timing)
    {
        timing ??= MushroomTiming.Default;
        RemainingDays = timing.RegrowthMinDays + (timing.RegrowthMaxDays - timing.RegrowthMinDays) * Next();
    }
}
