using GameNetcodeStuff;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items;

public class SnowballGun : NetworkBehaviour
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
        if (other == null || throwingPlayer == null) return;
        if (GameNetworkManager.Instance.localPlayerController != throwingPlayer) return;

        if (SnowballManager.HandleEnemyHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandlePlayerHitFromPlayer(other, transform.position, throwingPlayer)
            || SnowballManager.HandleSnowmanHit(other))
        {
            deactivated = true;
            DestroySnowballServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySnowballServerRpc() => Destroy(gameObject);
}
