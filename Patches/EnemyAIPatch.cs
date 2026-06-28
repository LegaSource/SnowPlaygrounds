using HarmonyLib;

namespace SnowPlaygrounds.Patches;

public class EnemyAIPatch
{
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
    [HarmonyPrefix]
    public static bool PlayerIsTargetable(ref bool __result) => PlayerControllerBPatch.isTargetable || (__result = false);
}
