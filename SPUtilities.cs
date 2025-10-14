using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Behaviours.Shaders;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Patches;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds;

public class SPUtilities
{
    public static Coroutine freezeEnemyCoroutine;
    public static Coroutine targetableCoroutine;

    public static Snowman SpawnSnowman(Vector3 position, Quaternion rotation)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Spawn(true);

            return networkObject.gameObject.GetComponentInChildren<Snowman>();
        }
        return null;
    }

    public static void SpawnSnowPile(Vector3 position, Quaternion rotation)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowPileObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    public static void FreezeEnemy(EnemyAI enemy, float duration, float slowdownFactor)
    {
        if (freezeEnemyCoroutine != null) enemy.StopCoroutine(freezeEnemyCoroutine);
        freezeEnemyCoroutine = enemy.StartCoroutine(FreezeEnemyCoroutine(enemy, duration, slowdownFactor));
    }

    private static IEnumerator FreezeEnemyCoroutine(EnemyAI enemy, float duration, float slowdownFactor)
    {
        CustomPassManager.SetupAuraForObjects([enemy.gameObject], SnowPlaygrounds.snowShader, $"{SnowPlaygrounds.modName}SnowballFreeze");
        EnemySpeedBehaviour speedBehaviour = enemy.GetComponent<EnemySpeedBehaviour>();
        speedBehaviour?.AddSpeedData(SnowPlaygrounds.modName, (1f / slowdownFactor) - 1, enemy.agent.speed);

        yield return new WaitForSeconds(duration);

        speedBehaviour?.RemoveSpeedData(SnowPlaygrounds.modName);
        CustomPassManager.RemoveAuraFromObjects([enemy.gameObject], $"{SnowPlaygrounds.modName}SnowballFreeze");
        freezeEnemyCoroutine = null;
    }

    public static void SetTargetable(PlayerControllerB player, bool targetable, float duration = 0f)
    {
        if (targetableCoroutine != null)
        {
            player.StopCoroutine(targetableCoroutine);
            targetableCoroutine = null;
        }

        if (duration > 0f) targetableCoroutine = player.StartCoroutine(TargetableCoroutine(targetable, duration));
        else PlayerControllerBPatch.isTargetable = targetable;
    }

    private static IEnumerator TargetableCoroutine(bool targetable, float duration)
    {
        yield return new WaitForSeconds(duration);

        PlayerControllerBPatch.isTargetable = targetable;
        targetableCoroutine = null;
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
        PlayAudio(SnowPlaygrounds.snowPoofAudio, position);

        GameObject particleObj = Object.Instantiate(SnowPlaygrounds.snowballParticle, position, rotation);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        Object.Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
    }

    public static void PlaySnowmanParticle(Vector3 position, Quaternion rotation)
    {
        PlayAudio(SnowPlaygrounds.snowPoofAudio, position);

        GameObject particleObj = Object.Instantiate(SnowPlaygrounds.snowmanParticle, position, rotation);
        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        Object.Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
    }

    public static void PlayAudio(GameObject audioPrefab, Vector3 position, float volume = 1f)
    {
        GameObject audioObject = Object.Instantiate(audioPrefab, position, Quaternion.identity);
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.volume = volume;
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
                snowman.ExitSnowmanEveryoneRpc((int)snowman.hidingPlayer.playerClientId);
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
        SnowPlaygrounds.snowballDecals.ToList().ForEach(Object.Destroy);
        SnowPlaygrounds.snowballDecals.Clear();
    }
}
