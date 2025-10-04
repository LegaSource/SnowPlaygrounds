using GameNetcodeStuff;
using LegaFusionCore.Registries;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Managers;

public class SnowPlaygroundsNetworkManager : NetworkBehaviour
{
    public static SnowPlaygroundsNetworkManager Instance;

    public void Awake() => Instance = this;

    [ServerRpc(RequireOwnership = false)]
    public void SpawnSnowmanFromSnowballServerRpc(int playerId, NetworkObjectReference obj, Vector3 position, Quaternion rotation)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        SnowballPlayer snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as SnowballPlayer;
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.transform.localScale = Constants.SNOWMAN_SCALE / ConfigManager.amountSnowballToBuild.Value * snowball.currentStackedItems;
            NetworkObject spawnedNetworkObject = gameObject.GetComponent<NetworkObject>();
            spawnedNetworkObject.Spawn(true);

            SpawnSnowmanClientRpc(playerId, spawnedNetworkObject, snowball.currentStackedItems);
            snowball.DestroySnowballClientRpc();
        }
    }

    [ClientRpc]
    public void SpawnSnowmanClientRpc(int playerId, NetworkObjectReference obj, int nbSnowball)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
        snowman.currentStackedSnowball = nbSnowball;
        snowman.RefreshHoverTip();

        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        if (Physics.Raycast(player.gameplayCamera.transform.position + player.gameplayCamera.transform.forward, Vector3.down, out RaycastHit hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
        {
            PlayerPhysicsRegion physicsRegion = hitInfo.collider.gameObject.transform.GetComponentInChildren<PlayerPhysicsRegion>();
            if (physicsRegion?.parentNetworkObject != null && physicsRegion.allowDroppingItems && physicsRegion.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
            {
                snowman.transform.SetParent(physicsRegion.physicsTransform);
                snowman.transform.localPosition = physicsRegion.physicsTransform.InverseTransformPoint(hitInfo.point + (Vector3.up * 0.04f) + physicsRegion.addPositionOffsetToItems);
                snowman.transform.rotation = Quaternion.Euler(0f, player.transform.rotation.eulerAngles.y, player.transform.rotation.eulerAngles.z);
            }
        }
    }

    [ClientRpc]
    public void AddFakeSnowmanClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
        snowman.snowmanTrigger.interactable = false;
        snowman.isEnemyHiding = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySnowmanServerRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
        Destroy(snowman.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SnowballFreezeEnemyServerRpc(NetworkObjectReference enemyObject, Vector3 position, Quaternion rotation)
        => SnowballFreezeEnemyClientRpc(enemyObject, position, rotation);

    [ClientRpc]
    public void SnowballFreezeEnemyClientRpc(NetworkObjectReference enemyObject, Vector3 position, Quaternion rotation, bool isEnemySnowball = false)
    {
        if (!enemyObject.TryGet(out NetworkObject networkObject)) return;

        SPUtilities.SnowballImpact(position, rotation);
        EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
        if (enemy != null)
        {
            if (enemy is FrostbiteAI frostbite) frostbite.HitFrostbite(isEnemySnowball);
            else SPUtilities.FreezeEnemy(enemy, ConfigManager.snowballSlowdownDuration.Value, ConfigManager.snowballSlowdownFactor.Value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SnowballHitPlayerServerRpc(int playerId, Vector3 force, Vector3 position) => SnowballHitPlayerClientRpc(playerId, force, position);

    [ClientRpc]
    public void SnowballHitPlayerClientRpc(int playerId, Vector3 force, Vector3 position)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        SPUtilities.SnowballImpact(position, player.transform.rotation);

        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            _ = player.thisController.Move(force);
            HUDManager.Instance.flashFilter = Mathf.Min(1f, HUDManager.Instance.flashFilter + 0.4f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShootGlacialDecoyServerRpc(int playerId)
    {
        GameObject gameObject = Instantiate(SnowPlaygrounds.snowballGDObj, transform.position - (transform.forward * 0.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);
        SnowballGD snowballGD = gameObject.GetComponent<SnowballGD>();
        gameObject.GetComponent<NetworkObject>().Spawn();
        snowballGD.ShootSnowballClientRpc(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnGlacialDecoyServerRpc(int playerId, NetworkObjectReference enemyObj) => ApplyFrostClientRpc(playerId, enemyObj);

    [ClientRpc]
    public void ApplyFrostClientRpc(int playerId, NetworkObjectReference enemyObj)
    {
        if (!enemyObj.TryGet(out NetworkObject networkObjectEnemy)) return;

        EnemyAI enemy = networkObjectEnemy.gameObject.GetComponentInChildren<EnemyAI>();
        LFCStatusEffectRegistry.ApplyStatus(enemy.gameObject, LFCStatusEffectRegistry.StatusEffectType.FROST, playerId, 10, 100);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnGlacialDecoyServerRpc(int playerId, int targetId) => ApplyFrostClientRpc(playerId, targetId);

    [ClientRpc]
    public void ApplyFrostClientRpc(int playerId, int targetId)
    {
        PlayerControllerB targetedPlayer = StartOfRound.Instance.allPlayerObjects[targetId].GetComponent<PlayerControllerB>();
        LFCStatusEffectRegistry.ApplyStatus(targetedPlayer.gameObject, LFCStatusEffectRegistry.StatusEffectType.FROST, playerId, 10, 10);
    }
}
