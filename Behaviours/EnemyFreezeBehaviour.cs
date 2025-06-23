using SnowPlaygrounds.Managers;
using System.Collections;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours;

public class EnemyFreezeBehaviour : MonoBehaviour
{
    public EnemyAI enemy;
    public Coroutine freezeCoroutine;
    public float originalSpeed;
    public float slowedSpeed;

    public void StartFreeze(float duration, float slowdownFactor)
    {
        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeCoroutine(duration, slowdownFactor));
    }

    private IEnumerator FreezeCoroutine(float duration, float slowdownFactor)
    {
        if (enemy != null)
        {
            originalSpeed = enemy.agent.speed;
            slowedSpeed = enemy.agent.speed / slowdownFactor;
            CustomPassManager.SetupAuraForObjects([enemy.gameObject], SnowPlaygrounds.frozenShader);

            yield return new WaitForSeconds(duration);

            CustomPassManager.RemoveAuraFromObjects([enemy.gameObject]);
        }
        freezeCoroutine = null;
    }

    private void LateUpdate()
    {
        if (freezeCoroutine == null || enemy == null) return;

        enemy.agent.speed = slowedSpeed;
        if (enemy is SandSpiderAI spider) spider.spiderSpeed = slowedSpeed;
    }
}
