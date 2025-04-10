using HarmonyLib;

namespace SnowPlaygrounds.Patches;

internal class EnemyAIPatch
{
    public static bool isTargetable = true;

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
    [HarmonyPrefix]
    private static bool IsPlayerTargetable(ref bool __result)
    {
        if (isTargetable) return true;

        __result = false;
        return false;
    }
}
