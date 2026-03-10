using GameNetcodeStuff;
using SnowPlaygrounds.Patches;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds;

public class SPUtilities
{
    public static Coroutine targetableCoroutine;

    public static void SpawnSnowman(Vector3 position, Quaternion rotation)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    public static void SpawnSnowPile(Vector3 position, Quaternion rotation)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.snowPileObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    public static void SpawnIceZone(Vector3 position, Quaternion rotation)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Object.Instantiate(SnowPlaygrounds.iceZoneObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }

    public static void SetTargetable(PlayerControllerB player, bool isTargetable)
    {
        if (isTargetable)
        {
            if (targetableCoroutine != null)
                player.StopCoroutine(targetableCoroutine);
            targetableCoroutine = player.StartCoroutine(TargetableCoroutine(isTargetable));
            return;
        }
        PlayerControllerBPatch.isTargetable = isTargetable;
    }

    private static IEnumerator TargetableCoroutine(bool isTargetable)
    {
        yield return new WaitForSeconds(3f);

        PlayerControllerBPatch.isTargetable = isTargetable;
        targetableCoroutine = null;
    }

    public static void SnowBallImpact(Vector3 position, Quaternion rotation)
    {
        PlayAudio(SnowPlaygrounds.snowPoofAudio, position);

        GameObject particleObj = Object.Instantiate(SnowPlaygrounds.snowParticle, position, rotation);
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
}
