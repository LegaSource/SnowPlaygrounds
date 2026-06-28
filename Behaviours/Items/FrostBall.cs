using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Managers;
using LethalStatus.Managers;
using LethalStatus.StatusEffects;
using SnowPlaygrounds.Behaviours.Enemies;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class FrostBall : LFCBouncyAoEProjectile
{
    protected override void PlayExplosionFx(Vector3 position, Quaternion rotation)
    {
        GameObject obj = Instantiate(SnowPlaygrounds.frostExplosionParticle, position, rotation);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        Destroy(obj, ps != null ? ps.main.duration : 2f);
    }

    protected override void PlayExplosionSfx(Vector3 position)
        => LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionAudio.name}", position);

    protected override void OnAffectPlayerServer(PlayerControllerB player)
        => LSNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, (int)player.playerClientId, (int)LSStatusEffectRegistry.StatusEffectType.FROST, 10, 10);

    protected override void OnAffectEnemyServer(EnemyAI enemy)
    {
        if (enemy is FrostbiteAI frostbite)
        {
            frostbite.HitFrostbiteEveryoneRpc(isFrostBall: true);
            return;
        }
        LSNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, enemy.NetworkObject, (int)LSStatusEffectRegistry.StatusEffectType.FROST, 10, 100);
    }
}
