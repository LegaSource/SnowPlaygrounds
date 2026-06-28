using AddonFusion.Behaviours.AddonComponents;
using AddonFusion.Behaviours.Scripts;
using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using SnowPlaygrounds;
using SnowPlaygrounds.Managers;
using UnityEngine;
using static AddonFusion.Behaviours.Scripts.AddonTargetDatabase;

namespace SnowPlaygroundsAddon.Behaviours.AddonComponents;

[AddonInfo(AddonTargetType.ALL)]
public class FrostMark : AddonComponent
{
    public override string AddonName => Constants.FROST_MARK;
    public override bool IsPassive => false;

    private readonly Collider[] overlapBuffer = new Collider[64];
    public readonly float AoERadius = 1f;
    public readonly int AoEMask = 1084754248;

    public AoEProjector frostProjector;

    public override void ActivateAddonAbility()
    {
        if (!onCooldown && StartOfRound.Instance.shipHasLanded && grabbableObject.playerHeldBy != null)
        {
            isEnabled = !isEnabled;
            if (isEnabled)
            {
                GameObject projectorObj = Instantiate(SnowPlaygroundsAddon.frostProjectorObj, grabbableObject.playerHeldBy.transform.position, Quaternion.identity);
                frostProjector = projectorObj.GetComponent<AoEProjector>();
            }
            else if (frostProjector != null)
            {
                if (frostProjector.TryConfirm(out Vector3 position))
                {
                    int count = Physics.OverlapSphereNonAlloc(position, AoERadius, overlapBuffer, AoEMask, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < count; i++)
                    {
                        Collider collider = overlapBuffer[i];
                        if (collider != null)
                        {
                            if (collider.gameObject.TryGetComponent(out PlayerControllerB player) && !player.isPlayerDead && LFCUtilities.ShouldNotBeLocalPlayer(player))
                                SnowPlaygroundsNetworkManager.Instance.SpawnFrostMarkEveryoneRpc((int)player.playerClientId, (int)LFCUtilities.LocalPlayer.playerClientId);
                            if (collider.gameObject.TryGetComponent(out EnemyAICollisionDetect collision) && collision.mainScript != null && !collision.mainScript.isEnemyDead)
                                SnowPlaygroundsNetworkManager.Instance.SpawnFrostMarkEveryoneRpc(collision.mainScript.NetworkObject, (int)LFCUtilities.LocalPlayer.playerClientId);
                        }
                    }
                }
                Destroy(frostProjector.gameObject);
                StartCooldown(ConfigManager.frostMarkCooldown.Value);
            }
        }
    }

    public void Update()
    {
        if (isEnabled && (grabbableObject == null || !grabbableObject.isHeld || grabbableObject.isPocketed) && frostProjector != null)
        {
            isEnabled = false;
            Destroy(frostProjector.gameObject);
        }
    }
}