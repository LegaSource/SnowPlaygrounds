using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using System;
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

    public void GrabSnowballs()
    {
        GrabbableObject grabbableObject = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer;
        if (grabbableObject != null && grabbableObject is Snowgun snowgun && snowgun.currentStackedItems < ConfigManager.snowgunAmount.Value)
        {
            int nbSnowball = ConfigManager.snowgunAmount.Value - snowgun.currentStackedItems;
            RemoveSnowballEveryoneRpc(nbSnowball);
            snowgun.UpdateStackedItemsEveryoneRpc(nbSnowball);
            return;
        }
        ForceGrabObjectServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ForceGrabObjectServerRpc(int playerId)
    {
        try
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            GameObject gameObject = Instantiate(SnowPlaygrounds.snowballPlayerObj, player.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
            GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
            grabbableObject.fallTime = 0f;
            NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Spawn();

            ForceGrabObjectEveryoneRpc(networkObject, playerId);
        }
        catch (Exception arg)
        {
            SnowPlaygrounds.mls.LogError($"Error in ForceGrabObjectServerRpc: {arg}");
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ForceGrabObjectEveryoneRpc(NetworkObjectReference obj, int playerId)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        SnowballPlayer snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as SnowballPlayer;
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        if (player == GameNetworkManager.Instance.localPlayerController) GrabObject(snowball, player);
    }

    public void GrabObject(SnowballPlayer snowball, PlayerControllerB player)
    {
        snowball.originalScale = snowball.transform.lossyScale;

        player.currentlyGrabbingObject = snowball;
        player.grabInvalidated = false;

        player.currentlyGrabbingObject.InteractItem();

        if (player.currentlyGrabbingObject.grabbable && player.FirstEmptyItemSlot() != -1)
        {
            player.playerBodyAnimator.SetBool("GrabInvalidated", value: false);
            player.playerBodyAnimator.SetBool("GrabValidated", value: false);
            player.playerBodyAnimator.SetBool("cancelHolding", value: false);
            player.playerBodyAnimator.ResetTrigger("Throw");
            player.SetSpecialGrabAnimationBool(setTrue: true);
            player.isGrabbingObjectAnimation = true;

            player.carryWeight = Mathf.Clamp(player.carryWeight + (player.currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
            player.grabObjectAnimationTime = player.currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f
                ? player.currentlyGrabbingObject.itemProperties.grabAnimationTime
                : 0.4f;

            if (!player.isTestingPlayer) player.GrabObjectServerRpc(player.currentlyGrabbingObject.NetworkObject);
            if (player.grabObjectCoroutine != null) player.StopCoroutine(player.grabObjectCoroutine);
            player.grabObjectCoroutine = player.StartCoroutine(player.GrabObject());
            snowball.InitializeEveryoneRpc(Mathf.Min(ConfigManager.snowballAmount.Value, currentStackedItems));
            RemoveSnowballEveryoneRpc(ConfigManager.snowballAmount.Value);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void RemoveSnowballEveryoneRpc(int nbSnowball)
    {
        float factor = 0.05f * nbSnowball;
        transform.localScale -= initialScale * Mathf.Max(factor, 0f);
        currentStackedItems -= nbSnowball;

        if (currentStackedItems <= 0 && LFCUtilities.IsServer)
            Destroy(gameObject);
    }
}
