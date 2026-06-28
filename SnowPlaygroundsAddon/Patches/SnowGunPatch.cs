using AddonFusion;
using HarmonyLib;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygroundsAddon.Behaviours.AddonComponents;

namespace SnowPlaygroundsAddon.Patches;

public class SnowGunPatch
{
    [HarmonyPatch(typeof(SnowGun), nameof(SnowGun.InitializeEveryoneRpc))]
    [HarmonyPostfix]
    public static void InitializeForEveryone(SnowGun __instance) => AFUtilities.SetAddonComponent<FrostMark>(__instance);

    [HarmonyPatch(typeof(SnowGun), nameof(SnowGun.SetControlTipsForItem))]
    [HarmonyPostfix]
    public static void SetControlTipsForAddon(SnowGun __instance) => AFUtilities.SetControlTipsForAddon(__instance);
}
