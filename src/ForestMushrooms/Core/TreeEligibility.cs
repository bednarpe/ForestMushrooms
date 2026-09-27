using System;

namespace ForestMushrooms.Core;

public static class TreeEligibility
{
    public const int RequiredNeighbors = 4;
    public static bool IsHost(bool hasTreeSpec, string templateName, bool requiresStandingWater = false) => hasTreeSpec
        && !requiresStandingWater
        && !string.IsNullOrEmpty(templateName)
        && templateName.IndexOf("Succulent", StringComparison.OrdinalIgnoreCase) < 0
        && templateName.IndexOf("Mangrove", StringComparison.OrdinalIgnoreCase) < 0;

    public static int CountNeighbors(Func<int, int, bool> hasTree)
    {
        int count = 0;
        for (int i = 0; i < RequiredNeighbors; i++)
        {
            var offset = Offset(i);
            if (hasTree(offset.X, offset.Y)) count++;
        }
        return count;
    }

    public static (int X, int Y) Offset(int index) => index switch
    {
        0 => (-1, 0), 1 => (1, 0), 2 => (0, -1), 3 => (0, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
