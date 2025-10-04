using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Addons;
using SnowPlaygrounds.Managers;

namespace SnowPlaygrounds.Behaviours.Items;

public class GlacialDecoy : AddonComponent
{
    public override void ActivateAddonAbility()
    {
        if (onCooldown || !StartOfRound.Instance.shipHasLanded) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player == null) return;

        StartCooldown(ConfigManager.glacialDecoyCooldown.Value);
        SnowPlaygroundsNetworkManager.Instance.ShootGlacialDecoyServerRpc((int)player.playerClientId);
    }
}