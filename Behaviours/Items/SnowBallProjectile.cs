using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Behaviours.Shaders;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowBallProjectile : NetworkBehaviour, IHittable
{
    public Rigidbody rigidbody;
    public PlayerControllerB throwingPlayer;

    public bool deactivated;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ThrowFromPositionEveryoneRpc(int playerId, Vector3 startPosition, Vector3 direction, float speed, float angleDeg)
    {
        if (!deactivated && rigidbody != null)
        {
            throwingPlayer = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            transform.position = startPosition;
            rigidbody.position = startPosition;
            rigidbody.velocity = Vector3.zero;
            rigidbody.AddForce(ComputeArcVelocity(direction, speed, angleDeg), ForceMode.VelocityChange);
        }
    }

    private static Vector3 ComputeArcVelocity(Vector3 direction, float speed, float angleDeg)
    {
        // Séparation des composantes horizontales et verticales
        Vector3 horizontal = new Vector3(direction.x, 0f, direction.z);
        float horizontalDistance = horizontal.magnitude;
        if (horizontalDistance <= 0.0001f)
            return Vector3.up * speed;

        // Calcul de l'angle de lancement (en radians) pour créer un arc
        float angle = angleDeg * Mathf.Deg2Rad;
        float time = horizontalDistance / (speed * Mathf.Cos(angle));

        // Calcul des vitesses initiales
        float verticalVelocity = (direction.y / time) - (0.5f * Physics.gravity.y * time);
        Vector3 horizontalVelocity = horizontal.normalized * (speed * Mathf.Cos(angle));
        return horizontalVelocity + (Vector3.up * verticalVelocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || deactivated || !LFCUtilities.IsServer || rigidbody == null) return;
        if (collision.collider != null && (collision.collider.gameObject.TryGetComponentInParent(out PlayerControllerB _) || collision.collider.gameObject.TryGetComponentInParent(out EnemyAI _)))
            return;

        ContactPoint cp = collision.GetContact(0);
        Vector3 point = cp.point;
        Vector3 normal = cp.normal;

        ExplodeServerRpc();
        ApplySnowDecalEveryoneRpc(point, normal);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void ApplySnowDecalEveryoneRpc(Vector3 point, Vector3 normal)
    {
        GameObject snowDecal = Instantiate(SnowPlaygrounds.snowDecal);
        snowDecal.transform.position = point + (normal * 0.01f);
        snowDecal.transform.forward = normal;
        _ = SnowPlaygrounds.snowDecals.Add(snowDecal);

        SPUtilities.SnowBallImpact(snowDecal.transform.position, Quaternion.LookRotation(normal));
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider != null && !deactivated)
        {
            if (LFCUtilities.IsServer)
            {
                if (collider.TryGetComponent(out EnemyAICollisionDetect collisionDetect) && collisionDetect.mainScript != null)
                {
                    DeactivateProjectile();
                    ExplodeServerRpc();
                    HandleEnemyHitEveryoneRpc(collisionDetect.mainScript.NetworkObject, transform.position);
                    return;
                }
                if (collider.TryGetComponent(out Snowman snowman) && snowman != null)
                {
                    DeactivateProjectile();
                    ExplodeServerRpc();
                    if (snowman.isEnemyHiding)
                        snowman.SpawnFrostbiteServerRpc();
                    else if (snowman.hidingPlayer != null)
                        snowman.ExitSnowmanEveryoneRpc((int)snowman.hidingPlayer.playerClientId);
                    else
                        HandleSnowmanHitEveryoneRpc(snowman.GetComponent<NetworkObject>());
                    return;
                }
            }
            if (collider.TryGetComponent(out PlayerControllerB player) && LFCUtilities.ShouldBeLocalPlayer(player) && player != throwingPlayer)
            {
                ExplodeServerRpc();
                HandlePlayerHitEveryoneRpc((int)player.playerClientId, transform.position);
            }
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void HandleEnemyHitEveryoneRpc(NetworkObjectReference enemyObject, Vector3 position)
    {
        if (enemyObject.TryGet(out NetworkObject networkObject) && networkObject.gameObject.TryGetComponentInChildren(out EnemyAI enemy))
        {
            SPUtilities.SnowBallImpact(position, enemy.transform.rotation);
            if (enemy is FrostbiteAI frostbite)
                frostbite.HitFrostbiteForEveryone();
            else
                _ = StartCoroutine(FreezeEnemyCoroutine(enemy));
        }
    }

    private IEnumerator FreezeEnemyCoroutine(EnemyAI enemy)
    {
        CustomPassManager.SetupAuraForObjects([enemy.gameObject], SnowPlaygrounds.snowShader, $"{SnowPlaygrounds.modName}SnowBallFreeze");
        LFCEnemySpeedBehaviour speedBehaviour = enemy.GetComponent<LFCEnemySpeedBehaviour>();
        speedBehaviour?.AddSpeedData(SnowPlaygrounds.modName, (1f / ConfigManager.snowBallSlowdownFactor.Value) - 1, enemy.agent.speed);

        yield return new WaitForSeconds(ConfigManager.snowBallSlowdownDuration.Value);

        speedBehaviour?.RemoveSpeedData(SnowPlaygrounds.modName);
        CustomPassManager.RemoveAuraFromObjects([enemy.gameObject], $"{SnowPlaygrounds.modName}SnowBallFreeze");
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void HandlePlayerHitEveryoneRpc(int playerId, Vector3 position)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SPUtilities.SnowBallImpact(position, player.transform.rotation);

        if (LFCUtilities.ShouldBeLocalPlayer(player))
        {
            Vector3 force = (player.transform.position - throwingPlayer.transform.position).normalized * ConfigManager.snowBallPushForce.Value;
            _ = player.thisController.Move(force);
            HUDManager.Instance.flashFilter = Mathf.Min(1f, HUDManager.Instance.flashFilter + 0.4f);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void HandleSnowmanHitEveryoneRpc(NetworkObjectReference snowmanObject)
    {
        if (snowmanObject.TryGet(out NetworkObject networkObject) && networkObject.gameObject.TryGetComponentInChildren(out Snowman snowman))
            Destroy(snowman.gameObject);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void ExplodeServerRpc()
    {
        if (!deactivated)
        {
            DeactivateProjectile();
            ExplodeEveryoneRpc();
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void ExplodeEveryoneRpc()
    {
        if (!LFCUtilities.IsServer)
            DeactivateProjectile();
    }

    private void DeactivateProjectile()
    {
        deactivated = true;

        foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>())
            Destroy(particleSystem);
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
            Destroy(meshRenderer);
        foreach (TrailRenderer trailRenderer in GetComponentsInChildren<TrailRenderer>())
            Destroy(trailRenderer);
        foreach (Collider collider in GetComponentsInChildren<Collider>())
            Destroy(collider);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer is Shovel)
            ExplodeServerRpc();
        return true;
    }
}
