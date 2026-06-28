using HarmonyLib;
using LegaFusionCore.Managers;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace SnowPlaygrounds.Patches;

public class RoundManagerPatch
{
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnMapObjects))]
    [HarmonyPostfix]
    public static void SpawnInsideHazards(RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        LFCUtilities.Shuffle(__instance.insideAINodes);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
    [HarmonyPostfix]
    public static void SpawnOutsideHazards(RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        if (!ConfigManager.anyLevel.Value && !ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name.ToLowerInvariant()) && !ConfigManager.spawnWeathers.Value.Contains(__instance.currentLevel.currentWeather.ToString())) return;

        LFCUtilities.Shuffle(__instance.outsideAINodes);
        if (ConfigManager.isIceZoneOutside.Value)
            LFCMapObjectsManager.SpawnOutsideMapObjectsForServer(ConfigManager.minIceZoneOutside.Value, ConfigManager.maxIceZoneOutside.Value, SPUtilities.SpawnIceZone);
        if (ConfigManager.isSnowmanOutside.Value)
            LFCMapObjectsManager.SpawnOutsideMapObjectsForServer(ConfigManager.minSnowmanOutside.Value, ConfigManager.maxSnowmanOutside.Value, SPUtilities.SpawnSnowman);
        if (ConfigManager.isSnowPileOutside.Value)
            LFCMapObjectsManager.SpawnOutsideMapObjectsForServer(ConfigManager.minSnowPileOutside.Value, ConfigManager.maxSnowPileOutside.Value, SPUtilities.SpawnSnowPile);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GeneratedFloorPostProcessing))]
    [HarmonyPostfix]
    public static void AddFakeSnowman(RoundManager __instance)
    {
        if (!__instance.IsHost || (!ConfigManager.anyLevel.Value && !ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name.ToLowerInvariant()) && !ConfigManager.spawnWeathers.Value.Contains(__instance.currentLevel.currentWeather.ToString())))
            return;

        System.Random random = new System.Random();
        List<Snowman> randomSnowmen = UnityEngine.Object.FindObjectsOfType<Snowman>()
            .OrderBy(s => random.Next())
            .Take(random.Next(ConfigManager.minFakeSnowman.Value, ConfigManager.maxFakeSnowman.Value))
            .ToList();
        randomSnowmen.ForEach(s => SnowPlaygroundsNetworkManager.Instance.AddFakeSnowmanEveryoneRpc(s.GetComponent<NetworkObject>()));
    }
}
