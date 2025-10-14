using LegaFusionCore.Utilities;
using SnowPlaygrounds.Managers;
using System;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class Snowgun : PhysicsProp
{
    public int currentStackedItems;
    public float lastShootTimer = 0f;
    public static float shootCooldown = 1.5f;

    public void InitializeForServer()
    {
        int value = UnityEngine.Random.Range(20, 50);
        InitializeEveryoneRpc(value);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int value)
    {
        SetScrapValue(value);
        LFCUtilities.SetAddonComponent<GlacialDecoy>(this, Constants.GLACIAL_DECOY);
        currentStackedItems = ConfigManager.snowgunAmount.Value;
    }

    public override void Update()
    {
        base.Update();
        lastShootTimer += Time.deltaTime;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!buttonDown || playerHeldBy == null) return;
        if (currentStackedItems > 0) ShootGunServerRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ShootGunServerRpc()
    {
        if (lastShootTimer < shootCooldown) return;

        GameObject gameObject = Instantiate(SnowPlaygrounds.snowballGunObj, transform.position - (transform.forward * 0.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);
        SnowballGun snowballGun = gameObject.GetComponent<SnowballGun>();
        gameObject.GetComponent<NetworkObject>().Spawn();
        snowballGun.ShootSnowballEveryoneRpc((int)playerHeldBy.playerClientId);
        UpdateStackedItemsEveryoneRpc(-1);

        lastShootTimer = 0f;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void UpdateStackedItemsEveryoneRpc(int nbSnowball)
    {
        currentStackedItems += nbSnowball;
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
