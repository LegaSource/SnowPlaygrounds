using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Items;
using System;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.MapObjects
{
    public class SnowPile : NetworkBehaviour
    {
        public InteractTrigger trigger;

        public void GrabSnowballs()
            => ForceGrabObjectServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

        [ServerRpc(RequireOwnership = false)]
        public void ForceGrabObjectServerRpc(int playerId)
        {
            try
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                GameObject gameObject = Instantiate(SnowPlaygrounds.snowballObj, player.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
                GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
                grabbableObject.fallTime = 0f;
                NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Spawn();
                ForceGrabObjectClientRpc(networkObject, playerId);
            }
            catch (Exception arg)
            {
                SnowPlaygrounds.mls.LogError($"Error in ForceGrabObjectServerRpc: {arg}");
            }
        }

        [ClientRpc]
        public void ForceGrabObjectClientRpc(NetworkObjectReference obj, int playerId)
        {
            if (obj.TryGet(out var networkObject))
            {
                Snowball snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as Snowball;
                snowball.Start();

                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                if (player == GameNetworkManager.Instance.localPlayerController)
                    GrabObject(snowball, player);
            }
        }

        public void GrabObject(Snowball snowball, PlayerControllerB player)
        {
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

                if (!player.isTestingPlayer)
                    player.GrabObjectServerRpc(player.currentlyGrabbingObject.NetworkObject);
                if (player.grabObjectCoroutine != null)
                    player.StopCoroutine(player.grabObjectCoroutine);
                player.grabObjectCoroutine = player.StartCoroutine(player.GrabObject());
            }
        }
    }
}
