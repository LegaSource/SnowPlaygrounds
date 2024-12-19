using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Managers
{
    public class SnowPlaygroundsNetworkManager : NetworkBehaviour
    {
        public static SnowPlaygroundsNetworkManager Instance;

        public void Awake() => Instance = this;

        [ServerRpc(RequireOwnership = false)]
        public void SpawnSnowmanServerRpc(NetworkObjectReference obj, Vector3 position)
        {
            if (obj.TryGet(out var networkObject))
            {
                Snowball snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as Snowball;
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    GameObject gameObject = Instantiate(SnowPlaygrounds.snowmanObj, hit.point + Vector3.down * 0.5f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                    gameObject.transform.localScale = Constants.SNOWMAN_SCALE / ConfigManager.amountSnowballToBuild.Value * snowball.currentStackedItems;
                    NetworkObject spawnedNetworkObject = gameObject.GetComponent<NetworkObject>();
                    spawnedNetworkObject.Spawn(true);
                    SpawnSnowmanClientRpc(spawnedNetworkObject, snowball.currentStackedItems);

                    DestroyObjectClientRpc(obj);
                }
            }
        }

        [ClientRpc]
        public void SpawnSnowmanClientRpc(NetworkObjectReference obj, int nbSnowball)
        {
            if (obj.TryGet(out var networkObject))
            {
                Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
                snowman.currentStackedSnowball = nbSnowball;
                snowman.RefreshHoverTip();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroySnowmanServerRpc(NetworkObjectReference obj) => DestroySnowmanClientRpc(obj);

        [ClientRpc]
        public void DestroySnowmanClientRpc(NetworkObjectReference obj)
        {
            if (obj.TryGet(out var networkObject))
            {
                Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
                Destroy(snowman.gameObject);
            }
        }

        [ClientRpc]
        public void DestroyObjectClientRpc(NetworkObjectReference obj)
        {
            if (obj.TryGet(out var networkObject))
            {
                GrabbableObject grabbableObject = networkObject.gameObject.GetComponentInChildren<GrabbableObject>();
                if (grabbableObject is Snowball snowball)
                    snowball.isThrown = true;
                grabbableObject.DestroyObjectInHand(grabbableObject.playerHeldBy);
            }
        }
    }
}
