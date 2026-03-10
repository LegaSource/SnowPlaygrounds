using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.MapObjects;

public class IceZone : NetworkBehaviour
{
    private bool hasLastPosition;
    private Vector3 lastPosition;
    private Vector3 slideVelocity;

    public float slideInertia = 5f; // Plus haut = suit moins la direction
    public float slideFriction = 0.99f; // Plus proche de 1 = glisse plus longtemps
    public float maxSlideSpeed = 5f;
    public float forwardBoost = 2f;
    public float reverseSteerFactor = 0.03f; // Plus proche de 0 = patine fort
    public float slipStrength = 4f;
    public float minSpeedToSlip = 0.2f;
    public float driftFrequency = 1.2f; // Plus haut = plus aléatoire

    private bool wasGrounded;
    private Coroutine groundCheckCoroutine;

    private readonly Dictionary<ulong, int> entityOverlapCount = [];

    private void OnTriggerStay(Collider collider)
    {
        if (collider != null)
        {
            if (LFCUtilities.IsServer && collider.TryGetComponent(out EnemyAICollisionDetect collisionDetect) && collisionDetect.mainScript != null)
            {
                if (AddEntityIce(collisionDetect.mainScript.NetworkObjectId))
                    collisionDetect.mainScript.GetComponent<LFCEnemySpeedBehaviour>()?.AddSpeedData($"{SnowPlaygrounds.modName}IceZone", 0.5f, collisionDetect.mainScript.agent.speed);
                return;
            }
            if (collider.TryGetComponent(out PlayerControllerB player) && LFCUtilities.ShouldBeLocalPlayer(player))
                ApplyPlayerIce(player);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider != null)
        {
            if (LFCUtilities.IsServer && collider.TryGetComponent(out EnemyAICollisionDetect collisionDetect) && collisionDetect.mainScript != null)
            {
                RemoveEntityIce(collisionDetect.mainScript.NetworkObjectId,
                    () => collisionDetect.mainScript.GetComponent<LFCEnemySpeedBehaviour>()?.RemoveSpeedData($"{SnowPlaygrounds.modName}IceZone"));
                return;
            }
            if (collider.TryGetComponent(out PlayerControllerB player) && LFCUtilities.ShouldBeLocalPlayer(player))
            {
                RemoveEntityIce(player.playerClientId, () =>
                {
                    LFCStatRegistry.RemoveModifier(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone{GetInstanceID()}");
                    hasLastPosition = false;
                    slideVelocity = Vector3.zero;
                });
            }
        }
    }

    private bool AddEntityIce(ulong id)
    {
        _ = entityOverlapCount.TryGetValue(id, out int amountEntity);
        amountEntity++;
        entityOverlapCount[id] = amountEntity;
        return amountEntity == 1;
    }

    private void RemoveEntityIce(ulong id, Action removeEntityIce)
    {
        if (entityOverlapCount.TryGetValue(id, out int amountEntity))
        {
            amountEntity--;
            if (amountEntity <= 0)
            {
                _ = entityOverlapCount.Remove(id);
                removeEntityIce.Invoke();
                return;
            }
            entityOverlapCount[id] = amountEntity;
        }
    }

    private void ApplyPlayerIce(PlayerControllerB player)
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || groundCheckCoroutine != null) return;

        int id = GetInstanceID();
        if (!LFCStatRegistry.HasModifier(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone{id}"))
        {
            if (AddEntityIce(player.playerClientId))
            {
                // Déjà dans une zone de glace, éviter de faire plus glisser le joueur pour des comportements trop aléatoires
                if (LFCStatRegistry.HasModifierWithTagPrefix(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone"))
                    return;

                LFCStatRegistry.AddModifier(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone{id}", -0.75f);
                wasGrounded = player.thisController.isGrounded;
                hasLastPosition = false;
                slideVelocity = Vector3.zero;
            }
            return;
        }

        if (!wasGrounded && player.thisController.isGrounded)
        {
            groundCheckCoroutine = StartCoroutine(GroundCheckCoroutine(player));
            return;
        }
        wasGrounded = player.thisController.isGrounded;

        Vector3 currentPos = player.transform.position;
        if (!hasLastPosition)
        {
            hasLastPosition = true;
            lastPosition = currentPos;
            return;
        }

        Vector3 observedVel = (currentPos - lastPosition) / deltaTime;
        lastPosition = currentPos;
        Vector3 baseVel = observedVel - slideVelocity;
        Vector3 baseHoriz = new Vector3(baseVel.x, 0f, baseVel.z);

        // Si le joueur ne pousse plus : friction seulement
        if (baseHoriz.sqrMagnitude < (minSpeedToSlip * minSpeedToSlip))
        {
            slideVelocity *= Mathf.Pow(slideFriction, deltaTime * 60f);
            slideVelocity.y = 0f;
            if (!LFCPlayerActionRegistry.IsLocked("Move"))
                _ = player.thisController.Move(slideVelocity * deltaTime);
            return;
        }

        Vector3 forwardDirection = baseHoriz.normalized;
        Vector3 sideDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized;
        float noise = Mathf.Sin((Time.time * (2f + driftFrequency)) + ((int)player.playerClientId * 0.01f));
        float speed = Mathf.Min(maxSlideSpeed, baseHoriz.magnitude + forwardBoost);
        Vector3 targetVelocity = (forwardDirection * speed) + (sideDirection * (noise * slipStrength));

        float align = 1f;
        Vector3 slideHoriz = new Vector3(slideVelocity.x, 0f, slideVelocity.z);
        Vector3 targetHoriz = new Vector3(targetVelocity.x, 0f, targetVelocity.z);

        if (slideHoriz.sqrMagnitude > 0.0001f && targetHoriz.sqrMagnitude > 0.0001f)
            align = Vector3.Dot(slideHoriz.normalized, targetHoriz.normalized);

        // Patinage
        float steerFactor = Mathf.Lerp(reverseSteerFactor, 1f, Mathf.InverseLerp(-0.2f, 0.8f, align));
        float steerFollow = (1f - Mathf.Exp(-slideInertia * deltaTime)) * steerFactor;

        slideVelocity = Vector3.Lerp(slideVelocity, targetVelocity, steerFollow);

        // Friction
        slideVelocity *= Mathf.Pow(slideFriction, deltaTime * 60f);
        slideVelocity.y = 0f;
        if (!LFCPlayerActionRegistry.IsLocked("Move"))
            _ = player.thisController.Move(slideVelocity * deltaTime);
    }

    private IEnumerator GroundCheckCoroutine(PlayerControllerB player)
    {
        player.sprintMeter = 0f;
        player.sprintMeterUI.fillAmount = player.sprintMeter;

        yield return new WaitForSeconds(2f);
        wasGrounded = true;
        groundCheckCoroutine = null;
    }
}
