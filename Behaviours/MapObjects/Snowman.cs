using GameNetcodeStuff;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.MapObjects;

public class Snowman : NetworkBehaviour, IHittable
{
    public InteractTrigger snowmanTrigger;
    public Camera camera;
    public Transform cameraPivot;
    private Camera playerCamera;

    public int currentStackedSnowBall = 0;

    public bool isPlayerHiding = false;
    public PlayerControllerB hidingPlayer;

    public bool isEnemyHiding = false;

    private void Start()
    {
        if (currentStackedSnowBall == 0)
            currentStackedSnowBall = ConfigManager.amountSnowBallToBuild.Value;
        playerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
    }

    public void Update()
    {
        if (!isEnemyHiding) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 7f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);
        foreach (Collider hitCollider in hitColliders)
        {
            PlayerControllerB player = hitCollider.GetComponent<PlayerControllerB>();
            if (LFCUtilities.ShouldBeLocalPlayer(player))
            {
                SpawnFrostbiteServerRpc();
                break;
            }
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SpawnFrostbiteServerRpc()
    {
        SpawnFrostbiteEveryoneRpc();

        GameObject gameObject = Instantiate(SnowPlaygrounds.frostbiteEnemy.enemyPrefab, transform.position, transform.rotation);
        gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
        gameObject.GetComponent<FrostbiteAI>().moveTowardsDestination = true;
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnFrostbiteEveryoneRpc()
    {
        if (ConfigManager.isJumpscareOn.Value)
            SPUtilities.PlayAudio(SnowPlaygrounds.jumpscareAudio, transform.position, ConfigManager.jumpscareVolume.Value);
        if (LFCUtilities.IsServer)
            Destroy(gameObject);
    }

    public void SnowmanInteraction()
    {
        if (currentStackedSnowBall < ConfigManager.amountSnowBallToBuild.Value)
            BuildSnowman();
        else if (hidingPlayer == null)
            EnterSnowmanEveryoneRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }

    public void BuildSnowman()
    {
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (LFCUtilities.ShouldBeLocalPlayer(player))
        {
            int nbSnowBall = 0;

            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject grabbableObject = player.ItemSlots[i];
                if (grabbableObject != null && grabbableObject is SnowBallItem snowBallItem)
                {
                    nbSnowBall += snowBallItem.currentStackedItems;
                    player.DestroyItemInSlotAndSync(i);
                }
            }

            if (nbSnowBall > 0) BuildSnowmanEveryoneRpc(nbSnowBall);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void BuildSnowmanEveryoneRpc(int nbSnowBall)
    {
        currentStackedSnowBall += nbSnowBall;
        gameObject.transform.localScale = currentStackedSnowBall >= ConfigManager.amountSnowBallToBuild.Value
            ? Constants.SNOWMAN_SCALE
            : Constants.SNOWMAN_SCALE / ConfigManager.amountSnowBallToBuild.Value * currentStackedSnowBall;
        RefreshHoverTip();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void EnterSnowmanEveryoneRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        player.DropAllHeldItems();

        snowmanTrigger.interactable = false;

        isPlayerHiding = true;
        hidingPlayer = player;

        transform.SetParent(player.transform);
        transform.position = player.transform.position;
        transform.rotation = player.transform.rotation;

        if (LFCUtilities.ShouldBeLocalPlayer(player))
        {
            camera.enabled = true;
            player.gameplayCamera = camera;

            SPUtilities.SetTargetable(player, false);
            HUDManager.Instance.ChangeControlTip(0, "Exit Snowman : [Q]", clearAllOther: true);
            LFCPlayerActionRegistry.AddLock("Move", $"{SnowPlaygrounds.modName}{gameObject.name}");
            LFCPlayerActionRegistry.AddLock("Jump", $"{SnowPlaygrounds.modName}{gameObject.name}");
            LFCPlayerActionRegistry.AddLock("Crouch", $"{SnowPlaygrounds.modName}{gameObject.name}");
        }
    }

    public void ExitSnowman() => ExitSnowmanEveryoneRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void ExitSnowmanEveryoneRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();

        transform.SetParent(null);

        if (LFCUtilities.ShouldBeLocalPlayer(player))
        {
            camera.enabled = false;
            player.gameplayCamera = playerCamera;

            HUDManager.Instance.ClearControlTips();
            LFCPlayerActionRegistry.RemoveLock("Move", $"{SnowPlaygrounds.modName}{gameObject.name}");
            LFCPlayerActionRegistry.RemoveLock("Jump", $"{SnowPlaygrounds.modName}{gameObject.name}");
            LFCPlayerActionRegistry.RemoveLock("Crouch", $"{SnowPlaygrounds.modName}{gameObject.name}");
            SPUtilities.SetTargetable(player, true);
        }
        if (LFCUtilities.IsServer) Destroy(gameObject);
    }

    public override void OnDestroy()
    {
        SPUtilities.PlaySnowmanParticle(transform.position, transform.rotation);
        base.OnDestroy();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && isPlayerHiding)
        {
            if (other.TryGetComponent(out EnemyAICollisionDetect collisionDetect))
                OnTriggerEnterEveryoneRpc(collisionDetect.mainScript.NetworkObject);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void OnTriggerEnterEveryoneRpc(NetworkObjectReference enemyObject)
    {
        if (enemyObject.TryGet(out NetworkObject networkObject) && networkObject.gameObject.TryGetComponentInChildren(out EnemyAI enemy))
        {
            if (LFCUtilities.ShouldBeLocalPlayer(hidingPlayer))
                ExitSnowman();
            if (enemy is FrostbiteAI frostbite)
                frostbite.HitFrostbiteForEveryone();
            else
                LFCNetworkManager.Instance.ApplyStatusEveryoneRpc((int)hidingPlayer.playerClientId, enemy.NetworkObject, (int)LFCStatusEffectRegistry.StatusEffectType.FROST, 10, 100);
        }
    }

    public void RefreshHoverTip()
        => snowmanTrigger.hoverTip = currentStackedSnowBall < ConfigManager.amountSnowBallToBuild.Value
            ? $"Add snowball {currentStackedSnowBall}/{ConfigManager.amountSnowBallToBuild.Value} : [LMB]"
            : "Enter : [LMB]";

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (currentStackedSnowBall >= ConfigManager.amountSnowBallToBuild.Value)
            SpawnSnowPileServerRpc();
        return true;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void SpawnSnowPileServerRpc()
    {
        SPUtilities.SpawnSnowPile(transform.position + Vector3.up, transform.rotation);
        Destroy(gameObject);
    }
}
