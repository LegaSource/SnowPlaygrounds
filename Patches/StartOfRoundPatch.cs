using HarmonyLib;
using SnowPlaygrounds.Managers;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches
{
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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        [HarmonyPostfix]
        public static void EndOfGame(ref StartOfRound __instance)
        {
            if (!__instance.IsServer) return;

            foreach (GameObject snowballDecal in SnowPlaygrounds.snowballDecals)
                Object.Destroy(snowballDecal);

            SnowPlaygrounds.snowballDecals.Clear();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDisable))]
        [HarmonyPostfix]
        public static void OnDisable() => SnowPlaygroundsNetworkManager.Instance = null;
    }
}
