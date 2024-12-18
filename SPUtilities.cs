using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours;
using SnowPlaygrounds.Patches;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds
{
    public class SPUtilities
    {
        public static Coroutine targetableCoroutine;

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
            if (targetableCoroutine != null)
                player.StopCoroutine(targetableCoroutine);
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
            if (targetableCoroutine != null)
                player.StopCoroutine(targetableCoroutine);
            EnemyAIPatch.isTargetable = false;

        }

        public static void ApplyDecal(Vector3 point, Vector3 normal)
        {
            GameObject snowballDecal = Object.Instantiate(SnowPlaygrounds.snowballDecal);
            snowballDecal.transform.position = point + normal * 0.01f;
            snowballDecal.transform.forward = normal;
            SnowPlaygrounds.snowballDecals.Add(snowballDecal);

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
    }
}
