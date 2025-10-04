using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
using SnowPlaygrounds.Managers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowballPlayer : PhysicsProp
{
    public int currentStackedItems = 0;
    public Rigidbody rigidbody;

    public bool isThrown = false;
    public PlayerControllerB throwingPlayer;

    public Coroutine throwCooldownCoroutine;

    [ServerRpc(RequireOwnership = false)]
    public void InitializeServerRpc(int nbSnowball) => InitializeClientRpc(nbSnowball);

    [ClientRpc]
    public void InitializeClientRpc(int nbSnowball) => currentStackedItems = nbSnowball;

    public override void Update()
    {
        if (!isThrown) base.Update();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || playerHeldBy == null) return;
        if (throwCooldownCoroutine == null)
        {
            throwCooldownCoroutine = StartCoroutine(ThrowCooldownCoroutine());
            SetControlTipsForItem();
        }
    }

    private IEnumerator ThrowCooldownCoroutine()
    {
        ThrowSnowballServerRpc();
        yield return new WaitForSeconds(ConfigManager.snowballThrowCooldown.Value);
        throwCooldownCoroutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropSnowballServerRpc(int playerId) => DropSnowballClientRpc(playerId, InstantiateSnowballToThrow());

    [ClientRpc]
    public void DropSnowballClientRpc(int playerId, NetworkObjectReference obj)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SnowballPlayer snowball = InitializeSnowballToThrow(obj, player);
        if (snowball == null) return;

        if (player.isInElevator) snowball.transform.SetParent(player.playersManager.elevatorTransform, worldPositionStays: true);
        if ((bool)snowball.transform.parent) snowball.startFallingPosition = snowball.transform.parent.InverseTransformPoint(snowball.startFallingPosition);
        snowball.FallToGround();

        _ = snowball.StartCoroutine(snowball.DetectGroundAndWalls());
    }

    [ServerRpc(RequireOwnership = false)]
    public void ThrowSnowballServerRpc() => ThrowSnowballClientRpc(InstantiateSnowballToThrow());

    [ClientRpc]
    public void ThrowSnowballClientRpc(NetworkObjectReference obj)
    {
        SnowballPlayer snowball = InitializeSnowballToThrow(obj);
        if (snowball == null) return;

        _ = snowball.throwingPlayer.gameplayCamera.transform.forward;
        SnowballManager.ThrowSnowballFromPlayer(snowball.throwingPlayer, snowball.rigidbody, 30f);

        _ = snowball.StartCoroutine(snowball.DetectGroundAndWalls());
    }

    public NetworkObject InstantiateSnowballToThrow()
    {
        NetworkObject networkObject;
        if (currentStackedItems > 1)
        {
            GameObject gameObject = Instantiate(SnowPlaygrounds.snowballPlayerObj, transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
            networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Spawn();
            currentStackedItems--;
            return networkObject;
        }
        HUDManager.Instance.ClearControlTips();
        return GetComponent<NetworkObject>();
    }

    public SnowballPlayer InitializeSnowballToThrow(NetworkObjectReference obj, PlayerControllerB player = null)
    {
        SnowballPlayer snowball = null;
        if (obj.TryGet(out NetworkObject networkObject))
        {
            snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as SnowballPlayer;
            snowball.isThrown = true;
            snowball.throwingPlayer = player ?? playerHeldBy;
            if (snowball.isHeld) snowball.throwingPlayer.DiscardHeldObject();
            // Fixer la position de la boule de neige
            snowball.transform.position = snowball.throwingPlayer.transform.position + (Vector3.up * 1.5f);
            snowball.startFallingPosition = snowball.transform.position;
        }
        return snowball;
    }

    public IEnumerator DetectGroundAndWalls()
    {
        while (isThrown)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitDown, 0.25f, 605030721, QueryTriggerInteraction.Collide))
            {
                SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
                _ = StartCoroutine(DestroyCoroutine());
                yield break;
            }
            yield return null;
        }
    }

    public IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (!deactivated) DestroyObjectInHand(throwingPlayer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !isThrown || throwingPlayer == null) return;
        if (GameNetworkManager.Instance.localPlayerController != throwingPlayer) return;

        if (SnowballManager.HandleEnemyHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandlePlayerHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandleSnowmanHit(other))
        {
            isThrown = false;
            LFCNetworkManager.Instance.DestroyObjectServerRpc(GetComponent<NetworkObject>());
        }
    }

    [ClientRpc]
    public void DestroySnowballClientRpc()
    {
        isThrown = true;
        DestroyObjectInHand(playerHeldBy);
    }

    public override void SetControlTipsForItem()
    {
        int index = Array.FindIndex(itemProperties.toolTips, s => s.StartsWith(Constants.SNOWBALL_AMOUNT, StringComparison.Ordinal));
        if (index >= 0)
        {
            itemProperties.toolTips[index] = $"{Constants.SNOWBALL_AMOUNT}{currentStackedItems}";
            HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
        }
    }
}
