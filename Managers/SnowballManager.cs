using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.MapObjects;
using UnityEngine;

namespace SnowPlaygrounds.Managers;

public static class SnowballManager
{
    public static void ThrowSnowballFromPlayer(PlayerControllerB player, Rigidbody rigidbody, float horizontalSpeed)
    {
        Vector3 throwDirection = player.gameplayCamera.transform.forward;
        float minY = -2f;
        if (throwDirection.y < minY) throwDirection = new Vector3(throwDirection.x, minY, throwDirection.z).normalized;
        Vector3 horizontalVelocity = throwDirection * horizontalSpeed; // Vitesse horizontale
        Vector3 verticalVelocity = new Vector3(0, 3f, 0); // Vitesse verticale pour créer l'arc

        // Réinitialisation de la vélocité avant d'appliquer la nouvelle force
        rigidbody.velocity = Vector3.zero;
        rigidbody.AddForce(horizontalVelocity, ForceMode.VelocityChange);
        rigidbody.AddForce(verticalVelocity, ForceMode.VelocityChange);
    }

    public static bool HandleEnemyHitFromPlayer(Collider other, Vector3 position, PlayerControllerB throwingPlayer)
    {
        EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
        if (enemyCollision == null) return false;

        SnowPlaygroundsNetworkManager.Instance.SnowballFreezeEnemyServerRpc(enemyCollision.mainScript.NetworkObject, other.ClosestPoint(position), throwingPlayer.transform.rotation);
        return true;
    }

    public static bool HandlePlayerHitFromPlayer(Collider other, Vector3 position, PlayerControllerB throwingPlayer)
    {
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null || player == throwingPlayer) return false;

        Vector3 force = (player.transform.position - throwingPlayer.transform.position).normalized * ConfigManager.snowballPushForce.Value;
        SnowPlaygroundsNetworkManager.Instance.SnowballHitPlayerServerRpc((int)player.playerClientId, force, other.ClosestPoint(position));

        return true;
    }

    public static bool HandleSnowmanHit(Collider other)
    {
        Snowman snowman = other.GetComponent<Snowman>();
        if (snowman == null) return false;

        if (snowman.isEnemyHiding) snowman.SpawnFrostbiteServerRpc();
        else if (snowman.hidingPlayer != null) snowman.ExitSnowmanClientRpc((int)snowman.hidingPlayer.playerClientId);
        else Object.Destroy(snowman.gameObject);

        return true;
    }
}
