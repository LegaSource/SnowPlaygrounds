﻿using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Enemies
{
    public class FrostbiteAI : EnemyAI
    {
        public float currentHitHP = 1f;
        public Transform TurnCompass;
        public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
        public AudioClip ThrowSound;
        public float moveTimer = 0f;

        public float lastShootTimer = 0f;
        public float shootCooldown = 0.5f;
        public bool hasHitPlayer = false;

        public enum State
        {
            WANDERING,
            CHASING,
            ATTACKING
        }

        public override void Start()
        {
            base.Start();

            currentBehaviourStateIndex = (int)State.WANDERING;
            creatureAnimator.SetTrigger("startMove");
            StartSearch(transform.position);

            SetEnemyOutside(transform.position.y > -80f);
        }

        public override void Update()
        {
            base.Update();

            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

            if (currentHitHP >= ConfigManager.frostbiteHitMax.Value)
                KillEnemyOnOwnerClient(!SnowPlaygrounds.isSellBodies);

            PlayMoveSound();
            int state = currentBehaviourStateIndex;
            if (targetPlayer != null && (state == (int)State.CHASING || state == (int)State.ATTACKING))
            {
                TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
            }
        }

        public void PlayMoveSound()
        {
            if (currentBehaviourStateIndex == (int)State.ATTACKING) return;

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
                case (int)State.WANDERING:
                    agent.speed = 2f * currentHitHP;
                    if (FoundClosestPlayerInRange(30f, 15f))
                    {
                        StopSearch(currentSearch);
                        DoAnimationClientRpc("startChase");
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                        return;
                    }
                    break;
                case (int)State.CHASING:
                    agent.speed = 4f * currentHitHP;
                    float distanceWithPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
                    if (!TargetClosestPlayerInAnyCase() || (distanceWithPlayer > 30f && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
                    {
                        StartSearch(transform.position);
                        DoAnimationClientRpc("startMove");
                        SwitchToBehaviourClientRpc((int)State.WANDERING);
                        return;
                    }
                    if (!hasHitPlayer && distanceWithPlayer <= 20f && CheckLineOfSightForPosition(targetPlayer.transform.position))
                    {
                        DoAnimationClientRpc("startThrow");
                        SwitchToBehaviourClientRpc((int)State.ATTACKING);
                        return;
                    }
                    SetMovingTowardsTargetPlayer(targetPlayer);
                    break;
                case (int)State.ATTACKING:
                    agent.speed = 0f;
                    if (hasHitPlayer || Vector3.Distance(transform.position, targetPlayer.transform.position) > 20f || !CheckLineOfSightForPosition(targetPlayer.transform.position))
                    {
                        lastShootTimer = 0f;
                        DoAnimationClientRpc("startChase");
                        SwitchToBehaviourClientRpc((int)State.CHASING);
                        return;
                    }
                    ShootPlayer(targetPlayer);
                    break;

                default:
                    break;
            }
        }

        public bool FoundClosestPlayerInRange(float range, float senseRange)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if (targetPlayer == null)
            {
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        public bool TargetClosestPlayerInAnyCase()
        {
            mostOptimalDistance = 2000f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if (targetPlayer == null) return false;
            return true;
        }

        public void ShootPlayer(PlayerControllerB player)
        {
            if (lastShootTimer >= shootCooldown)
            {
                PlayThrowClientRpc();

                GameObject gameObject = Instantiate(SnowPlaygrounds.snowballEnemyObj, transform.position + Vector3.up * 1.5f, Quaternion.identity, StartOfRound.Instance.propsContainer);
                SnowballEnemy snowballEnemy = gameObject.GetComponent<GrabbableObject>() as SnowballEnemy;
                gameObject.GetComponent<NetworkObject>().Spawn();
                snowballEnemy.ThrowSnowballClientRpc(thisNetworkObject, player.transform.position + Vector3.up * 1.5f);

                lastShootTimer = 0f;
                shootCooldown = UnityEngine.Random.Range(ConfigManager.frostbiteMinCooldown.Value, ConfigManager.frostbiteMaxCooldown.Value);
            }
            else
            {
                lastShootTimer += Time.deltaTime;
            }
        }

        [ClientRpc]
        public void PlayThrowClientRpc()
            => creatureSFX.PlayOneShot(ThrowSound);

        public override void OnCollideWithPlayer(Collider other)
        {
            PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
            if (player == null || player != GameNetworkManager.Instance.localPlayerController) return;
            if (currentBehaviourStateIndex != (int)State.CHASING && currentBehaviourStateIndex != (int)State.ATTACKING) return;

            player.KillPlayer(Vector3.zero, false, CauseOfDeath.Crushing);
            DoAnimationServerRpc("startMove");
            SwitchToBehaviourServerRpc((int)State.WANDERING);
        }

        public void HitFrostbite()
        {
            currentHitHP += ConfigManager.frostbiteHitIncrement.Value;
            StartCoroutine(HitCoroutine());
        }

        private IEnumerator HitCoroutine()
        {
            CustomPassManager.SetupCustomPassForEnemy(this);
            yield return new WaitForSeconds(0.2f);
            CustomPassManager.RemoveAura(this);
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            if (playHitSFX && enemyType.hitBodySFX != null)
            {
                creatureSFX.PlayOneShot(enemyType.hitBodySFX);
                WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.hitBodySFX);
            }
        }

        public override void OnDestroy()
        {
            SPUtilities.PlaySnowmanParticle(transform.position, transform.rotation);
            base.OnDestroy();
        }

        [ServerRpc(RequireOwnership = false)]
        public void DoAnimationServerRpc(string animationState)
            => DoAnimationClientRpc(animationState);

        [ClientRpc]
        public void DoAnimationClientRpc(string animationState)
            => creatureAnimator.SetTrigger(animationState);
    }
}
