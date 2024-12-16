using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours;
using SnowPlaygrounds.Patches;
using System.Collections;
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
    }
}
