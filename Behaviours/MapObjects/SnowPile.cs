using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.MapObjects;

public class SnowPile : NetworkBehaviour
{
    public int currentStackedItems;
    public InteractTrigger trigger;
    private Vector3 initialScale;

    private void Start()
    {
        currentStackedItems = ConfigManager.snowPileAmount.Value;
        initialScale = transform.localScale;
    }

    public void GrabSnowBalls()
    {
        GrabbableObject grabbableObject = GameNetworkManager.Instance.localPlayerController?.currentlyHeldObjectServer;
        if (grabbableObject != null && grabbableObject is SnowGun snowGun && snowGun.currentStackedItems < ConfigManager.snowGunAmount.Value)
        {
            int nbSnowBall = ConfigManager.snowGunAmount.Value - snowGun.currentStackedItems;
            RemoveSnowBallEveryoneRpc(nbSnowBall);
            snowGun.UpdateStackedItemsEveryoneRpc(nbSnowBall);
            return;
        }
        ForceGrabObjectServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ForceGrabObjectServerRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        GameObject gameObject = Instantiate(SnowPlaygrounds.snowBallItemObj, player.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);

        SnowBallItem snowBallItem = gameObject.GetComponent<SnowBallItem>();
        snowBallItem.fallTime = 0f;
        NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
        networkObject.Spawn();

        LFCNetworkManager.Instance.ForceGrabObjectEveryoneRpc(networkObject, (int)player.playerClientId);
        snowBallItem.InitializeEveryoneRpc(Mathf.Min(ConfigManager.snowBallAmount.Value, currentStackedItems));
        RemoveSnowBallEveryoneRpc(ConfigManager.snowBallAmount.Value);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RemoveSnowBallEveryoneRpc(int nbSnowBall)
    {
        float factor = 0.05f * nbSnowBall;
        transform.localScale -= initialScale * Mathf.Max(factor, 0f);
        currentStackedItems -= nbSnowBall;

        if (currentStackedItems <= 0 && LFCUtilities.IsServer)
            Destroy(gameObject);
    }
}
