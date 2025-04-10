using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Managers;

public class SnowPlaygroundsNetworkManager : NetworkBehaviour
{
    public static SnowPlaygroundsNetworkManager Instance;

    public void Awake()
        => Instance = this;

    [ServerRpc(RequireOwnership = false)]
    public void SpawnSnowmanServerRpc(int playerId, NetworkObjectReference obj, Vector3 position, Quaternion rotation)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        Snowball snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as Snowball;
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.Euler(0f, rotation.eulerAngles.y, rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.transform.localScale = Constants.SNOWMAN_SCALE / ConfigManager.amountSnowballToBuild.Value * snowball.currentStackedItems;
            NetworkObject spawnedNetworkObject = gameObject.GetComponent<NetworkObject>();
            spawnedNetworkObject.Spawn(true);
            SpawnSnowmanClientRpc(playerId, spawnedNetworkObject, snowball.currentStackedItems);

            DestroyObjectClientRpc(obj);
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
        => DestroySnowmanClientRpc(obj);

    [ClientRpc]
    public void DestroySnowmanClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
        Destroy(snowman.gameObject);
    }

    [ClientRpc]
    public void DestroyObjectClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        GrabbableObject grabbableObject = networkObject.gameObject.GetComponentInChildren<GrabbableObject>();
        if (grabbableObject is Snowball snowball) snowball.isThrown = true;
        grabbableObject.DestroyObjectInHand(grabbableObject.playerHeldBy);
    }
}
