using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowballEnemy : NetworkBehaviour, IHittable
{
    public Rigidbody rigidbody;
    public FrostbiteAI throwingEnemy;

    public bool deactivated = false;
    public bool hasBeenHit = false;

    [ClientRpc]
    public void ThrowSnowballClientRpc(NetworkObjectReference enemyObject, Vector3 targetPosition)
    {
        if (!enemyObject.TryGet(out NetworkObject networkObject)) return;

        throwingEnemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>() as FrostbiteAI;

        // Fixer la position de la boule de neige
        transform.position = throwingEnemy.transform.position + (Vector3.up * 1.5f);

        float speed = throwingEnemy.isOutside ? ConfigManager.frostbiteSnowballSpeedOutside.Value : ConfigManager.frostbiteSnowballSpeedInside.Value;
        Vector3 toTarget = targetPosition - transform.position;

        // Séparation des composantes horizontales et verticales
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = horizontal.magnitude;

        // Calcul de l'angle de lancement (en radians) pour créer un arc
        float angle = 45f * Mathf.Deg2Rad;
        float timeToReachTarget = horizontalDistance / (speed * Mathf.Cos(angle));

        // Calcul des vitesses initiales
        float verticalVelocity = (toTarget.y / timeToReachTarget) - (0.5f * Physics.gravity.y * timeToReachTarget);
        Vector3 horizontalVelocity = horizontal.normalized * (speed * Mathf.Cos(angle));

        // Ajout des forces pour le lancement
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(horizontalVelocity + (Vector3.up * verticalVelocity), ForceMode.VelocityChange);

        // Détection des collisions
        _ = StartCoroutine(DetectGroundAndWalls());
    }

    public IEnumerator DetectGroundAndWalls()
    {
        while (!deactivated)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitDown, 0.3f, 605030721, QueryTriggerInteraction.Collide))
            {
                SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
                _ = StartCoroutine(DestroyCoroutine());
                yield break;
            }
            yield return null;
        }
    }

    public IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (deactivated) yield break;

        deactivated = true;
        if (LFCUtilities.IsServer) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || throwingEnemy == null) return;
        if (!LFCUtilities.IsServer) return;

        if (HandleEnemyHit(other)) return;
        if (HandlePlayerHit(other)) return;
        if (SnowballManager.HandleSnowmanHit(other))
        {
            DestroySnowballClientRpc();
            Destroy(gameObject);
        }
    }

    private bool HandleEnemyHit(Collider other)
    {
        EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
        if (enemyCollision == null || (enemyCollision.mainScript is FrostbiteAI && !hasBeenHit)) return false;

        SnowPlaygroundsNetworkManager.Instance.SnowballFreezeEnemyClientRpc(enemyCollision.mainScript.NetworkObject, other.ClosestPoint(transform.position), throwingEnemy.transform.rotation, isEnemySnowball: true);
        return true;
    }

    private bool HandlePlayerHit(Collider other)
    {
        if (hasBeenHit) return false;

        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null) return false;

        throwingEnemy.hasHitPlayer = true;
        HitPlayerClientRpc((int)player.playerClientId, other.ClosestPoint(transform.position));
        return true;
    }

    [ClientRpc]
    public void HitPlayerClientRpc(int playerId, Vector3 position)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SPUtilities.SnowballImpact(position, player.transform.rotation);

        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            _ = throwingEnemy.StartCoroutine(throwingEnemy.FreezePlayerCoroutine());
            player.DamagePlayer((int)(ConfigManager.frostbiteSnowballDamage.Value * throwingEnemy.currentHitHP), hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
            HUDManager.Instance.flashFilter = 1f;
        }
        if (LFCUtilities.IsServer) Destroy(gameObject);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (playerWhoHit == null) return true;

        if (playerWhoHit.currentlyHeldObjectServer is Shovel)
            ThrowBackSnowballServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        else
            DestroySnowballServerRpc();

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ThrowBackSnowballServerRpc(int playerId) => ThrowBackSnowballClientRpc(playerId);

    [ClientRpc]
    public void ThrowBackSnowballClientRpc(int playerId)
    {
        hasBeenHit = true;
        SnowballManager.ThrowSnowballFromPlayer(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>(), rigidbody, 45f);

        _ = StartCoroutine(DetectGroundAndWalls());
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySnowballServerRpc()
    {
        Destroy(gameObject);
        DestroySnowballClientRpc();
    }

    [ClientRpc]
    public void DestroySnowballClientRpc() => deactivated = true;
}
