using HarmonyLib;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
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
        SPUtilities.Shuffle(__instance.insideAINodes);
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
    [HarmonyPostfix]
    private static void SpawnOutsideHazards(ref RoundManager __instance)
    {
        if (!__instance.IsHost) return;

        if (ConfigManager.anyLevel.Value || ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name))
        {
            SPUtilities.Shuffle(__instance.outsideAINodes);

            if (ConfigManager.isSnowPileOutside.Value)
                SpawnSnowPile(__instance);
            if (ConfigManager.isSnowmanOutside.Value)
                SpawnSnowman(__instance);
        }
    }

    private static void SpawnSnowPile(RoundManager roundManager)
    {
        System.Random random = new System.Random();
        for (int i = 0; i < random.Next(ConfigManager.minSnowPileOutside.Value, ConfigManager.maxSnowPileOutside.Value); i++)
        {
            GameObject[] outsideAINodes = roundManager.outsideAINodes;
            Vector3 position = outsideAINodes[random.Next(0, outsideAINodes.Length)].transform.position;
            position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random) + Vector3.up;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowPileObj, hit.point + Vector3.down, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                gameObject.transform.localScale *= 2.5f;
                gameObject.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    private static void SpawnSnowman(RoundManager roundManager)
    {
        System.Random random = new System.Random();
        for (int i = 0; i < random.Next(ConfigManager.minSnowmanOutside.Value, ConfigManager.maxSnowmanOutside.Value); i++)
        {
            GameObject[] outsideAINodes = roundManager.outsideAINodes;
            Vector3 position = outsideAINodes[random.Next(0, outsideAINodes.Length)].transform.position;
            position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random) + Vector3.up;
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                gameObject.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GeneratedFloorPostProcessing))]
    [HarmonyPostfix]
    private static void AddFakeSnowman(ref RoundManager __instance)
    {
        if (!__instance.IsHost) return;
        if (!ConfigManager.anyLevel.Value && !ConfigManager.spawnLevels.Value.Contains(__instance.currentLevel.name)) return;

        SnowPlaygrounds.snowmen.AddRange(Object.FindObjectsOfType<Snowman>());

        System.Random random = new System.Random();
        List<Snowman> randomSnowmen = SnowPlaygrounds.snowmen.OrderBy(s => random.Next()).Take(random.Next(ConfigManager.minFakeSnowman.Value, ConfigManager.maxFakeSnowman.Value)).ToList();
        foreach (Snowman snowman in randomSnowmen) SnowPlaygroundsNetworkManager.Instance.AddFakeSnowmanClientRpc(snowman.GetComponent<NetworkObject>());
    }
}
