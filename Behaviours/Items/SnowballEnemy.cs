using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowballEnemy : PhysicsProp
{
    public Rigidbody rigidbody;
    public FrostbiteAI throwingEnemy;

    public override void Update() { }

    [ClientRpc]
    public void ThrowSnowballClientRpc(NetworkObjectReference enemyObject, Vector3 targetPosition)
    {
        if (!enemyObject.TryGet(out NetworkObject networkObject)) return;

        throwingEnemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>() as FrostbiteAI;

        // Fixer la position de la boule de neige
        Start();
        transform.position = throwingEnemy.transform.position + (Vector3.up * 1.5f);
        startFallingPosition = transform.position;

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
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitDown, 0.25f, 605030721, QueryTriggerInteraction.Collide))
            {
                SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
                _ = StartCoroutine(DestroyCoroutine());
                break;
            }
            yield return null;
        }
    }

    public IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(1f);
        if (!deactivated) DestroyObjectInHand(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || throwingEnemy == null) return;

        if (HandleEnemyHit(other)) return;
        if (HandlePlayerHit(other)) return;
        if (HandleSnowmanHit(other)) return;
    }

    private bool HandleEnemyHit(Collider other)
    {
        if (!GameNetworkManager.Instance.localPlayerController.IsServer && !GameNetworkManager.Instance.localPlayerController.IsHost) return false;

        EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
        if (enemyCollision == null || enemyCollision.mainScript is FrostbiteAI) return false;

        FreezeEnemyClientRpc(enemyCollision.mainScript.NetworkObject, other.ClosestPoint(transform.position));
        DestroyObjectClientRpc();

        return true;
    }

    private bool HandlePlayerHit(Collider other)
    {
        if (!GameNetworkManager.Instance.localPlayerController.IsServer && !GameNetworkManager.Instance.localPlayerController.IsHost) return false;

        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null) return false;

        HitPlayerServerRpc((int)player.playerClientId, other.ClosestPoint(transform.position));
        DestroyObjectServerRpc();

        return true;
    }

    private bool HandleSnowmanHit(Collider other)
    {
        if (!GameNetworkManager.Instance.localPlayerController.IsServer && !GameNetworkManager.Instance.localPlayerController.IsHost) return false;

        Snowman snowman = other.GetComponent<Snowman>();
        if (snowman == null) return false;

        if (snowman.isEnemyHiding) snowman.SpawnFrostbiteServerRpc();
        else if (snowman.hidingPlayer != null) snowman.ExitSnowmanClientRpc((int)snowman.hidingPlayer.playerClientId);
        else SnowPlaygroundsNetworkManager.Instance.DestroySnowmanClientRpc(snowman.GetComponent<NetworkObject>());

        DestroyObjectClientRpc();

        return true;
    }

    [ClientRpc]
    public void FreezeEnemyClientRpc(NetworkObjectReference enemyObject, Vector3 position)
    {
        if (!enemyObject.TryGet(out NetworkObject networkObject)) return;

        SPUtilities.SnowballImpact(position, throwingEnemy.transform.rotation);

        EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
        if (enemy != null) SPUtilities.StartFreezeEnemy(enemy, ConfigManager.snowballSlowdownDuration.Value, ConfigManager.snowballSlowdownFactor.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HitPlayerServerRpc(int playerId, Vector3 position)
    {
        throwingEnemy.hasHitPlayer = true;
        HitPlayerClientRpc(playerId, position);
    }

    [ClientRpc]
    public void HitPlayerClientRpc(int playerId, Vector3 position)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SPUtilities.SnowballImpact(position, player.transform.rotation);

        if (player == GameNetworkManager.Instance.localPlayerController)
        {
            _ = StartCoroutine(ImmobilizePlayerCoroutine());
            player.DamagePlayer((int)(ConfigManager.frostbiteSnowballDamage.Value * throwingEnemy.currentHitHP), hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
            HUDManager.Instance.flashFilter = 1f;
        }
    }

    public IEnumerator ImmobilizePlayerCoroutine()
    {
        IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).Disable();
        IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint", false).Disable();

        yield return new WaitForSeconds(ConfigManager.frostbiteStunDuration.Value);

        FinalizePlayerHitServerRpc();
        IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).Enable();
        IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint", false).Enable();
    }

    [ServerRpc(RequireOwnership = false)]
    public void FinalizePlayerHitServerRpc()
        => throwingEnemy.hasHitPlayer = false;

    [ServerRpc(RequireOwnership = false)]
    public void DestroyObjectServerRpc()
        => DestroyObjectClientRpc();

    [ClientRpc]
    public void DestroyObjectClientRpc()
        => DestroyObjectInHand(null);
}
