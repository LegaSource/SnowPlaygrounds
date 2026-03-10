using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Shaders;
using LegaFusionCore.Managers;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Enemies;

public class FrostbiteAI : EnemyAI
{
    public Transform TurnCompass;
    public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
    public AudioClip ThrowSound;
    public Transform ThrowPoint;

    public float moveTimer = 0f;
    public float throwTimer = 0f;
    public float throwCooldown = 5f;

    public bool canThrow = false;

    public Coroutine throwCoroutine;
    public Coroutine attackCoroutine;

    public enum State { WANDERING, CHASING, THROWING }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.WANDERING;
        StartSearch(transform.position);
        SetEnemyOutside(transform.position.y > -80f);
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        PlayMoveSound();
        int state = currentBehaviourStateIndex;
        if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.THROWING))
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
        LFCUtilities.UpdateTimer(ref throwTimer, throwCooldown, !canThrow, () => canThrow = true);
    }

    public void PlayMoveSound()
    {
        if (currentBehaviourStateIndex == (int)State.THROWING) return;

        moveTimer -= Time.deltaTime;
        if (MoveSounds.Length > 0 && moveTimer <= 0)
        {
            creatureSFX.PlayOneShot(MoveSounds[UnityEngine.Random.Range(0, MoveSounds.Length)]);
            moveTimer = currentBehaviourStateIndex == (int)State.WANDERING ? 0.75f : 0.5f;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.WANDERING: DoWandering(); break;
            case (int)State.CHASING: DoChasing(); break;
            case (int)State.THROWING: DoThrowing(); break;
        }
    }

    public void DoWandering()
    {
        agent.speed = 2f;
        if (this.FoundClosestPlayerInRange(30, 15))
        {
            StopSearch(currentSearch);
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
    }

    public void DoChasing()
    {
        if (attackCoroutine != null) return;

        agent.speed = 4f;
        if (!this.TargetClosestPlayerInAnyCase(out float distanceWithPlayer) || (distanceWithPlayer > 30f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
        {
            StartSearch(transform.position);
            SwitchToBehaviourClientRpc((int)State.WANDERING);
            return;
        }
        if (CanThrow() && distanceWithPlayer <= 20f && CheckLineOfSightForPosition(targetPlayer.transform.position))
        {
            SwitchToBehaviourClientRpc((int)State.THROWING);
            return;
        }
        SetMovingTowardsTargetPlayer(targetPlayer);
    }

    public void DoThrowing()
    {
        if (throwCoroutine != null) return;

        agent.speed = 0f;
        if (!CanThrow() || Vector3.Distance(transform.position, targetPlayer.transform.position) > 30f || !CheckLineOfSightForPosition(targetPlayer.transform.position))
        {
            DoAnimationEveryoneRpc("startMove");
            SwitchToBehaviourClientRpc((int)State.CHASING);
            return;
        }
        throwCoroutine = StartCoroutine(ThrowCoroutine());
    }

    public IEnumerator ThrowCoroutine()
    {
        canThrow = false;
        DoAnimationEveryoneRpc("startThrow");
        yield return this.WaitForFullAnimation("throw");

        PlayThrowEveryoneRpc();
        GameObject gameObject = Instantiate(SnowPlaygrounds.frostBallObj, ThrowPoint.transform.position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<FrostBall>().ThrowFromPositionEveryoneRpc(entityId: NetworkObjectId,
            startPosition: ThrowPoint.transform.position,
            direction: targetPlayer.transform.position + (Vector3.up * 1.5f) - ThrowPoint.transform.position,
            isOutside: isOutside);

        throwCoroutine = null;
    }

    public void CancelThrowCoroutine()
    {
        if (throwCoroutine != null)
        {
            StopCoroutine(throwCoroutine);
            throwCoroutine = null;
        }
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.WANDERING)
        {
            PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
            if (LFCUtilities.ShouldBeLocalPlayer(player))
                AttackServerRpc((int)player.playerClientId);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void AttackServerRpc(int playerId)
    {
        if (currentBehaviourStateIndex != (int)State.WANDERING && attackCoroutine == null)
            attackCoroutine = StartCoroutine(AttackCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));
    }

    public IEnumerator AttackCoroutine(PlayerControllerB player)
    {
        if (ConfigManager.frostbiteEating.Value)
            LFCNetworkManager.Instance.KillPlayerEveryoneRpc((int)player.playerClientId, Vector3.zero, spawnBody: false, (int)CauseOfDeath.Mauling);
        else
            LFCNetworkManager.Instance.DamagePlayerEveryoneRpc((int)player.playerClientId, ConfigManager.frostbiteDamage.Value, hasDamageSFX: true, callRPC: true, (int)CauseOfDeath.Mauling);

        yield return new WaitForSeconds(1f);
        if (ConfigManager.frostbiteEating.Value)
            SwitchToBehaviourClientRpc((int)State.WANDERING);

        attackCoroutine = null;
    }

    public void CancelAttackCoroutine()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (!isEnemyDead && ConfigManager.frostbiteDefaultHit.Value)
            HitFrostbiteForEveryone();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void HitFrostbiteEveryoneRpc(bool isFrostBall = false) => HitFrostbiteForEveryone(isFrostBall);

    public void HitFrostbiteForEveryone(bool isFrostBall = false)
    {
        if (isFrostBall)
        {
            KillEnemyOnOwnerClient();
            if (LFCUtilities.IsServer)
                SpawnSnowGunServerRpc();
            return;
        }

        base.HitEnemy(force: 1, playerWhoHit: null, playHitSFX: false, hitID: -1);
        enemyHP--;
        if (enemyHP <= 0 && IsOwner)
        {
            KillEnemyOnOwnerClient();
            if (LFCUtilities.IsServer)
                SPUtilities.SpawnSnowPile(transform.position + Vector3.up, transform.rotation);
            return;
        }
        _ = StartCoroutine(HitCoroutine());
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SpawnSnowGunServerRpc()
    {
        SnowGun snowGun = LFCObjectsManager.SpawnObjectForServer(SnowPlaygrounds.snowGunObj, transform.position + (Vector3.up * 0.5f)) as SnowGun;
        snowGun.InitializeForServer();
    }

    private IEnumerator HitCoroutine()
    {
        CustomPassManager.SetupAuraForObjects([gameObject], SnowPlaygrounds.snowShader, $"{SnowPlaygrounds.modName}FrostbiteHit");
        yield return new WaitForSeconds(0.2f);
        CustomPassManager.RemoveAuraFromObjects([gameObject], $"{SnowPlaygrounds.modName}FrostbiteHit");
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy();

        if (LFCUtilities.IsServer)
        {
            CancelThrowCoroutine();
            CancelAttackCoroutine();
        }
    }

    public bool CanThrow() => canThrow && targetPlayer != null && !LFCStatusEffectRegistry.HasStatus(targetPlayer.gameObject, LFCStatusEffectRegistry.StatusEffectType.FROST);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void PlayThrowEveryoneRpc() => creatureSFX.PlayOneShot(ThrowSound);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void DoAnimationEveryoneRpc(string animationState) => creatureAnimator.SetTrigger(animationState);

    public override void OnDestroy()
    {
        SPUtilities.PlaySnowmanParticle(transform.position, transform.rotation);
        LFCStatRegistry.RemoveModifier(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}SnowBallEnemy");
        CustomPassManager.RemoveAuraFromObjects([gameObject]);

        base.OnDestroy();
    }
}
