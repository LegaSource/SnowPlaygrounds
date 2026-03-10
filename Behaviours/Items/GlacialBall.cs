using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Addons;
using SnowPlaygrounds.Managers;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class GlacialBall : AddonComponent
{
    public override void ActivateAddonAbility()
    {
        if (onCooldown || !StartOfRound.Instance.shipHasLanded) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player != null)
        {
            Vector3 position = player.localVisor.transform.position + player.gameplayCamera.transform.forward;
            StartCooldown(ConfigManager.glacialBallCooldown.Value);
            SnowPlaygroundsNetworkManager.Instance.ThrowFrostBallServerRpc((int)player.playerClientId, position, player.gameplayCamera.transform.forward);
        }
    }
}
