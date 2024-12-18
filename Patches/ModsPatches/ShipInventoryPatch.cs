using HarmonyLib;
using SnowPlaygrounds.Behaviours.Items;

namespace SnowPlaygrounds.Patches.ModsPatches
{
    [HarmonyPatch]
    internal class ShipInventoryPatch
    {
        public static bool PreStoreItem(GrabbableObject item) => item is not Snowball;
    }
}
