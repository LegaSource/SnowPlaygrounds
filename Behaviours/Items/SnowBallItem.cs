using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Managers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowBallItem : PhysicsProp
{
    public Rigidbody rigidbody;

    public int currentStackedItems = 0;
    public PlayerControllerB throwingPlayer;

    public Coroutine throwCooldownCoroutine;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int nbSnowBall) => currentStackedItems = nbSnowBall;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (buttonDown && playerHeldBy != null)
            throwCooldownCoroutine ??= StartCoroutine(ThrowCooldownCoroutine());
    }

    private IEnumerator ThrowCooldownCoroutine()
    {
        ThrowSnowBallServerRpc(direction: playerHeldBy.gameplayCamera.transform.forward, speed: 30f, angleDeg: 3f);
        yield return new WaitForSeconds(ConfigManager.snowBallThrowCooldown.Value);
        throwCooldownCoroutine = null;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ThrowSnowBallServerRpc(Vector3 direction, float speed, float angleDeg)
    {
        GameObject gameObject = Instantiate(SnowPlaygrounds.snowBallProjectileObj, transform.position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();

        SnowBallProjectile snowBallProjectile = gameObject.GetComponent<SnowBallProjectile>();
        snowBallProjectile.ThrowFromPositionEveryoneRpc(playerId: (int)playerHeldBy.playerClientId,
            startPosition: transform.position,
            direction: direction,
            speed: speed,
            angleDeg: angleDeg);
        UpdateStackedItemsEveryoneRpc();

        if (currentStackedItems <= 0)
            LFCNetworkManager.Instance.DestroyObjectEveryoneRpc(GetComponent<NetworkObject>());
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateStackedItemsEveryoneRpc()
    {
        currentStackedItems--;
        if (isHeld && !isPocketed && LFCUtilities.ShouldBeLocalPlayer(playerHeldBy))
            SetControlTipsForItem();
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
