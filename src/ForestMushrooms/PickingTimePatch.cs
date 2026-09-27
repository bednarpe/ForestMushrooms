using HarmonyLib;
using Timberborn.Yielding;

namespace ForestMushrooms;

[HarmonyPatch(typeof(Yielder), nameof(Yielder.RemovalTimeInHours), MethodType.Getter)]
public static class PickingTimePatch
{
    public static void Postfix(Yielder __instance, ref float __result)
    {
        if (__instance.ComponentName == "ForestMushrooms")
        {
            var tree = __instance.GetComponent<ForestMushroom>();
            if (tree != null) __result = (float)tree.Timing.PickingHours;
        }
    }
}
