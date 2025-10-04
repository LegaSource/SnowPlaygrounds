using HarmonyLib;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches;

internal class RoundManagerPatch
{
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnMapObjects))]
    [HarmonyPostfix]
    private static void SpawnInsideHazards(ref RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        LFCUtilities.Shuffle(__instance.insideAINodes);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
    [HarmonyPostfix]
    private static void SpawnOutsideHazards(ref RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        if (!ConfigManager.anyLevel.Value && !ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name.ToLowerInvariant()) && !ConfigManager.spawnWeathers.Value.Contains(__instance.currentLevel.currentWeather.ToString())) return;

        LFCUtilities.Shuffle(__instance.outsideAINodes);
        if (ConfigManager.isSnowmanOutside.Value)
            SpawnHazard(__instance, ConfigManager.minSnowmanOutside.Value, ConfigManager.maxSnowmanOutside.Value, (pos, rot) => SPUtilities.SpawnSnowman(pos, rot));
        if (ConfigManager.isSnowPileOutside.Value)
            SpawnHazard(__instance, ConfigManager.minSnowmanOutside.Value, ConfigManager.maxSnowPileOutside.Value, SPUtilities.SpawnSnowPile);
    }

    private static void SpawnHazard(RoundManager roundManager, int minHazard, int maxHazard, Action<Vector3, Quaternion> spawnMethod)
    {
        System.Random random = new System.Random();
        for (int i = 0; i < random.Next(minHazard, maxHazard); i++)
        {
            GameObject[] outsideAINodes = roundManager.outsideAINodes;
            Vector3 position = outsideAINodes[random.Next(0, outsideAINodes.Length)].transform.position;
            position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random) + Vector3.up;
            spawnMethod(position, Quaternion.identity);
        }
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GeneratedFloorPostProcessing))]
    [HarmonyPostfix]
    private static void AddFakeSnowman(ref RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        if (!ConfigManager.anyLevel.Value && !ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name.ToLowerInvariant()) && !ConfigManager.spawnWeathers.Value.Contains(__instance.currentLevel.currentWeather.ToString())) return;

        SnowPlaygrounds.snowmen.AddRange(UnityEngine.Object.FindObjectsOfType<Snowman>());

        System.Random random = new System.Random();
        List<Snowman> randomSnowmen = SnowPlaygrounds.snowmen.OrderBy(s => random.Next()).Take(random.Next(ConfigManager.minFakeSnowman.Value, ConfigManager.maxFakeSnowman.Value)).ToList();
        randomSnowmen.ForEach(s => SnowPlaygroundsNetworkManager.Instance.AddFakeSnowmanClientRpc(s.GetComponent<NetworkObject>()));
    }
}
