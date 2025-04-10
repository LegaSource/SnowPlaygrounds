using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using SnowPlaygrounds.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds;

public class SPUtilities
{
    public static Coroutine targetableCoroutine;

    public static void Shuffle<T>(IList<T> collection)
    {
        for (int i = collection.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (collection[randomIndex], collection[i]) = (collection[i], collection[randomIndex]);
        }
    }

    public static void StartFreezeEnemy(EnemyAI enemy, float duration, float slowdownFactor)
    {
        EnemyFreezeBehaviour freezeBehaviour = enemy.GetComponent<EnemyFreezeBehaviour>();
        if (freezeBehaviour == null)
        {
            freezeBehaviour = enemy.gameObject.AddComponent<EnemyFreezeBehaviour>();
            freezeBehaviour.enemy = enemy;
        }
        freezeBehaviour.StartFreeze(duration, slowdownFactor);
    }

    public static void StartTargetable(PlayerControllerB player, float duration)
    {
        if (targetableCoroutine != null) player.StopCoroutine(targetableCoroutine);
        targetableCoroutine = player.StartCoroutine(SetTargetablePlayerCoroutine(duration));
    }

    public static IEnumerator SetTargetablePlayerCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        EnemyAIPatch.isTargetable = true;
        targetableCoroutine = null;
    }

    public static void SetUntargetable(PlayerControllerB player)
    {
        if (targetableCoroutine != null) player.StopCoroutine(targetableCoroutine);
        EnemyAIPatch.isTargetable = false;
    }

    public static void ApplyDecal(Vector3 point, Vector3 normal)
    {
        GameObject snowballDecal = Object.Instantiate(SnowPlaygrounds.snowballDecal);
        snowballDecal.transform.position = point + (normal * 0.01f);
        snowballDecal.transform.forward = normal;
        _ = SnowPlaygrounds.snowballDecals.Add(snowballDecal);

        SnowballImpact(snowballDecal.transform.position, Quaternion.LookRotation(normal));
    }

    public static void SnowballImpact(Vector3 position, Quaternion rotation)
    {
        PlaySnowPoof(position);

        GameObject particleObj = Object.Instantiate(SnowPlaygrounds.snowballParticle, position, rotation);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        Object.Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
    }

    public static void PlaySnowPoof(Vector3 position)
    {
        GameObject audioObject = Object.Instantiate(SnowPlaygrounds.snowmanAudio, position, Quaternion.identity);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        Object.Destroy(audioObject, audioSource.clip.length);
    }

    public static void PlaySnowmanParticle(Vector3 position, Quaternion rotation)
    {
        PlaySnowPoof(position);

        GameObject particleObj = Object.Instantiate(SnowPlaygrounds.snowmanParticle, position, rotation);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        Object.Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
    }

    public static void PlayJumpscareAudio(Vector3 position)
    {
        GameObject audioObject = Object.Instantiate(SnowPlaygrounds.jumpscareAudio, position, Quaternion.identity);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.volume = ConfigManager.jumpscareVolume.Value;
        Object.Destroy(audioObject, audioSource.clip.length);
    }

    public static void DespawnSnowmanEndGame(StartOfRound startOfRound)
    {
        if (!startOfRound.IsHost) return;

        foreach (Snowman snowman in SnowPlaygrounds.snowmen)
        {
            if (snowman == null) continue;

            if (snowman.isPlayerHiding)
            {
                snowman.ExitSnowmanClientRpc((int)snowman.hidingPlayer.playerClientId);
                continue;
            }

            NetworkObject networkObject = snowman.GetComponent<NetworkObject>();
            if (networkObject == null || !networkObject.IsSpawned) continue;

            networkObject.Despawn();
        }

        SnowPlaygrounds.snowmen.Clear();
    }

    public static void ClearSnowballDecals()
    {
        foreach (GameObject snowballDecal in SnowPlaygrounds.snowballDecals.ToList()) Object.Destroy(snowballDecal);
        SnowPlaygrounds.snowballDecals.Clear();
    }
}
