using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowballGD : NetworkBehaviour
{
    public Rigidbody rigidbody;
    public PlayerControllerB throwingPlayer;
    public bool deactivated = false;

    [ClientRpc]
    public void ShootSnowballClientRpc(int playerId)
    {
        throwingPlayer = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        // Fixer la position de la boule de neige
        transform.position = throwingPlayer.currentlyHeldObjectServer.transform.position - (throwingPlayer.currentlyHeldObjectServer.transform.forward * 0.5f);
        SPUtilities.PlayAudio(SnowPlaygrounds.snowShootAudio, transform.position);
        SnowballManager.ThrowSnowballFromPlayer(throwingPlayer, rigidbody, 60f);

        _ = StartCoroutine(DetectGroundAndWalls(throwingPlayer));
    }

    public IEnumerator DetectGroundAndWalls(PlayerControllerB throwingPlayer)
    {
        while (!deactivated)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitDown, 0.3f, 605030721, QueryTriggerInteraction.Collide))
            {
                SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
                _ = StartCoroutine(DestroyCoroutine(hitDown.point + (hitDown.normal * 0.01f), throwingPlayer));
                yield break;
            }
            yield return null;
        }
    }

    public IEnumerator DestroyCoroutine(Vector3 position, PlayerControllerB throwingPlayer)
    {
        yield return new WaitForSeconds(0.5f);
        if (deactivated) yield break;

        deactivated = true;
        if (LFCUtilities.IsServer)
        {
            Snowman snowman = SPUtilities.SpawnSnowman(position, throwingPlayer.transform.rotation);
            if (snowman != null) SnowPlaygroundsNetworkManager.Instance.SpawnSnowmanClientRpc((int)throwingPlayer.playerClientId, snowman.GetComponent<NetworkObject>(), ConfigManager.amountSnowballToBuild.Value);

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || throwingPlayer == null) return;
        if (GameNetworkManager.Instance.localPlayerController != throwingPlayer) return;

        if (HandleEnemyHitFromPlayer(other, throwingPlayer)
            || HandlePlayerHitFromPlayer(other, throwingPlayer)
            || SnowballManager.HandleSnowmanHit(other))
        {
            deactivated = true;
            DestroySnowballServerRpc();
        }
    }

    public bool HandleEnemyHitFromPlayer(Collider other, PlayerControllerB throwingPlayer)
    {
        EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
        if (enemyCollision == null) return false;

        SnowPlaygroundsNetworkManager.Instance.SpawnGlacialDecoyServerRpc((int)throwingPlayer.playerClientId, enemyCollision.mainScript.NetworkObject);
        return true;
    }

    public bool HandlePlayerHitFromPlayer(Collider other, PlayerControllerB throwingPlayer)
    {
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (player == null || player == throwingPlayer) return false;

        SnowPlaygroundsNetworkManager.Instance.SpawnGlacialDecoyServerRpc((int)throwingPlayer.playerClientId, (int)player.playerClientId);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySnowballServerRpc() => Destroy(gameObject);
}
