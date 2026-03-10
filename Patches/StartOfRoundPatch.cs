using HarmonyLib;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches;

internal class StartOfRoundPatch
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyBefore(["evaisa.lethallib"])]
    [HarmonyPostfix]
    private static void StartRound(ref StartOfRound __instance)
    {
        if (NetworkManager.Singleton.IsHost && SnowPlaygroundsNetworkManager.Instance == null)
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.managerPrefab, __instance.transform.parent);
            gameObject.GetComponent<NetworkObject>().Spawn();
            SnowPlaygrounds.mls.LogInfo("Spawning SnowPlaygroundsNetworkManager");
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
    [HarmonyPostfix]
    public static void StartOfGame()
    {
        ClearSnowmen();
        ClearSnowDecals();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
    [HarmonyPostfix]
    public static void EndOfGame()
    {
        ClearSnowmen();
        ClearSnowDecals();
    }

    public static void ClearSnowmen()
    {
        if (LFCUtilities.IsServer)
        {
            foreach (Snowman snowman in Object.FindObjectsOfType<Snowman>())
            {
                if (snowman == null) continue;
                if (snowman.isPlayerHiding)
                {
                    snowman.ExitSnowmanEveryoneRpc((int)snowman.hidingPlayer.playerClientId);
                    continue;
                }

                NetworkObject networkObject = snowman.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                    networkObject.Despawn();
            }
        }
    }

    public static void ClearSnowDecals()
    {
        SnowPlaygrounds.snowDecals.ToList().ForEach(Object.Destroy);
        SnowPlaygrounds.snowDecals.Clear();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDisable))]
    [HarmonyPostfix]
    public static void OnDisable() => SnowPlaygroundsNetworkManager.Instance = null;
}
