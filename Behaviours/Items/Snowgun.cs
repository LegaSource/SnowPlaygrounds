using LegaFusionCore.Utilities;
using SnowPlaygrounds.Managers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowGun : PhysicsProp
{
    public int currentStackedItems;
    public Transform ShootPoint;
    public Coroutine shootCooldownCoroutine;

    public void InitializeForServer()
    {
        int value = UnityEngine.Random.Range(20, 50);
        string[] addons = [Constants.GLACIAL_DECOY, Constants.GLACIAL_BALL];
        InitializeEveryoneRpc(value, addons[UnityEngine.Random.Range(0, addons.Length)]);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int value, string addonName)
    {
        SetScrapValue(value);
        if (addonName.Equals(Constants.GLACIAL_DECOY))
            LFCUtilities.SetAddonComponent<GlacialDecoy>(this, addonName);
        else
            LFCUtilities.SetAddonComponent<GlacialBall>(this, addonName);
        currentStackedItems = ConfigManager.snowGunAmount.Value;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (buttonDown && playerHeldBy != null && currentStackedItems > 0)
            shootCooldownCoroutine ??= StartCoroutine(ShootCooldownCoroutine());
    }

    private IEnumerator ShootCooldownCoroutine()
    {
        ShootGunServerRpc(direction: playerHeldBy.gameplayCamera.transform.forward);
        yield return new WaitForSeconds(1.5f);
        shootCooldownCoroutine = null;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ShootGunServerRpc(Vector3 direction)
    {
        GameObject gameObject = Instantiate(SnowPlaygrounds.snowBallProjectileObj, ShootPoint.transform.position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        PlayThrowEveryoneRpc();

        SnowBallProjectile snowBallProjectile = gameObject.GetComponent<SnowBallProjectile>();
        snowBallProjectile.ThrowFromPositionEveryoneRpc(playerId: (int)playerHeldBy.playerClientId,
            startPosition: ShootPoint.transform.position,
            direction: direction,
            speed: 60f,
            angleDeg: 3f);
        UpdateStackedItemsEveryoneRpc(-1);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayThrowEveryoneRpc() => SPUtilities.PlayAudio(SnowPlaygrounds.snowShootAudio, transform.position);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateStackedItemsEveryoneRpc(int nbSnowBall)
    {
        currentStackedItems += nbSnowBall;
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
