using HarmonyLib;

namespace SnowPlaygrounds.Patches;

internal class EnemyAIPatch
{
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
    [HarmonyPrefix]
    private static bool PlayerIsTargetable(ref bool __result) => PlayerControllerBPatch.isTargetable || (__result = false);
}
