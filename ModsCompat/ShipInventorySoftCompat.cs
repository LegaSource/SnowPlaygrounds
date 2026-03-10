using HarmonyLib;
using SnowPlaygrounds.Behaviours.Items;
using System;
using System.Reflection;

namespace SnowPlaygrounds.ModsCompat;

public static class ShipInventorySoftCompat
{
    public static void Patch(Harmony harmony)
    {
        Type shipInventoryType = Type.GetType("ShipInventory.Helpers.ItemManager, ShipInventory");
        if (shipInventoryType != null)
        {
            MethodInfo storeItem = AccessTools.Method(shipInventoryType, "StoreItem");
            if (storeItem != null)
            {
                HarmonyMethod prefix = new HarmonyMethod(AccessTools.Method(typeof(ShipInventorySoftCompat), nameof(StoreItem)));
                _ = harmony.Patch(storeItem, prefix: prefix);
            }
        }
    }

    public static bool StoreItem(GrabbableObject item) => item is not SnowBallItem;
}
