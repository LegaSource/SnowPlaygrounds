using GameNetcodeStuff;
using LegaFusionCore.Managers;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Managers;

public class SnowPlaygroundsNetworkManager : NetworkBehaviour
{
    public static SnowPlaygroundsNetworkManager Instance;

    public void Awake() => Instance = this;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void AddFakeSnowmanEveryoneRpc(NetworkObjectReference obj)
    {
        if (obj.TryGet(out NetworkObject networkObject))
        {
            Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
            snowman.snowmanTrigger.interactable = false;
            snowman.isEnemyHiding = true;
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ThrowFrostBallServerRpc(int playerId, Vector3 position, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.5f)
            direction = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>().transform.forward;
        direction = direction.normalized;

        GameObject gameObject = Instantiate(SnowPlaygrounds.frostBallObj, position, Quaternion.identity);
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<FrostBall>().ThrowFromPlayerEveryoneRpc(playerId, position, direction);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SpawnSnowmanServerRpc(int playerId, int nbSnowBall)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        Vector3 position = player.gameplayCamera.transform.position + player.gameplayCamera.transform.forward;
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            GameObject gameObject = Instantiate(SnowPlaygrounds.snowmanObj, hit.point, Quaternion.Euler(0f, player.transform.rotation.eulerAngles.y, player.transform.rotation.eulerAngles.z), RoundManager.Instance.mapPropsContainer.transform);
            gameObject.transform.localScale = nbSnowBall >= ConfigManager.amountSnowBallToBuild.Value
                ? Constants.SNOWMAN_SCALE
                : Constants.SNOWMAN_SCALE / ConfigManager.amountSnowBallToBuild.Value * nbSnowBall;
            NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();
            networkObject.Spawn(true);

            SpawnSnowmanEveryoneRpc((int)player.playerClientId, networkObject, nbSnowBall);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnSnowmanEveryoneRpc(int playerId, NetworkObjectReference obj, int nbSnowBall)
    {
        if (obj.TryGet(out NetworkObject networkObject))
        {
            Snowman snowman = networkObject.gameObject.GetComponentInChildren<Snowman>();
            snowman.currentStackedSnowBall = nbSnowBall;
            snowman.RefreshHoverTip();
            LFCMapObjectsManager.AttachMapObjectForEveryone(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>(), snowman.gameObject);
        }
    }
}
