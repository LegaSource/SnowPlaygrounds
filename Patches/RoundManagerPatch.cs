using HarmonyLib;
using SnowPlaygrounds.Managers;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
        [HarmonyPostfix]
        private static void LoadNewGame(ref RoundManager __instance)
        {
            if (__instance.IsHost)
            {
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
                    GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowmanObj, hit.point + Vector3.down * 0.5f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                    gameObject.GetComponent<NetworkObject>().Spawn(true);
                }
            }
        }
    }
}
