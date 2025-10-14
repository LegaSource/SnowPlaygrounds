using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Utilities;
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

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int nbSnowball) => currentStackedItems = nbSnowball;

    public override void Update()
    {
        if (!isThrown) base.Update();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || playerHeldBy == null) return;
        throwCooldownCoroutine ??= StartCoroutine(ThrowCooldownCoroutine());
    }

    private IEnumerator ThrowCooldownCoroutine()
    {
        ThrowSnowballServerRpc();
        yield return new WaitForSeconds(ConfigManager.snowballThrowCooldown.Value);
        throwCooldownCoroutine = null;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void DropSnowballServerRpc(int playerId) => DropSnowballEveryoneRpc(playerId, InstantiateSnowballToThrow());

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DropSnowballEveryoneRpc(int playerId, NetworkObjectReference obj)
    {
        UpdateStackedItems();

        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SnowballPlayer snowball = InitializeSnowballToThrow(obj, player);
        if (snowball == null) return;

        if (player.isInElevator) snowball.transform.SetParent(player.playersManager.elevatorTransform, worldPositionStays: true);
        if ((bool)snowball.transform.parent) snowball.startFallingPosition = snowball.transform.parent.InverseTransformPoint(snowball.startFallingPosition);
        snowball.FallToGround();

        _ = snowball.StartCoroutine(snowball.DetectGroundAndWalls());
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ThrowSnowballServerRpc() => ThrowSnowballEveryoneRpc(InstantiateSnowballToThrow());

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ThrowSnowballEveryoneRpc(NetworkObjectReference obj)
    {
        UpdateStackedItems();

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
            return networkObject;
        }
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
        if (!LFCUtilities.IsServer) return;

        if (SnowballManager.HandleEnemyHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandlePlayerHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandleSnowmanHit(other))
        {
            isThrown = false;
            LFCNetworkManager.Instance.DestroyObjectEveryoneRpc(GetComponent<NetworkObject>());
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DestroySnowballEveryoneRpc()
    {
        isThrown = true;
        DestroyObjectInHand(playerHeldBy);
    }

    public void UpdateStackedItems()
    {
        currentStackedItems--;
        if (isHeld && !isPocketed && playerHeldBy != null && playerHeldBy == GameNetworkManager.Instance.localPlayerController) SetControlTipsForItem();
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
