﻿using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Managers;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.MapObjects
{
    public class Snowman : NetworkBehaviour
    {
        public InteractTrigger snowmanTrigger;
        public Camera camera;
        public Transform cameraPivot;
        private Camera playerCamera;

        public int currentStackedSnowball = 0;

        public bool isPlayerHiding = false;
        public PlayerControllerB hidingPlayer;

        private void Start()
        {
            if (currentStackedSnowball == 0)
                currentStackedSnowball = ConfigManager.amountSnowballToBuild.Value;

            playerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        }

        public void SnowmanInteraction()
        {
            if (currentStackedSnowball < ConfigManager.amountSnowballToBuild.Value)
                BuildSnowman();
            else if (hidingPlayer == null)
                EnterSnowmanServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
        }

        public void BuildSnowman()
        {
            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            int nbSnowball = 0;

            for (int i = 0; i < player.ItemSlots.Length; i++)
            {
                GrabbableObject grabbableObject = player.ItemSlots[i];
                if (grabbableObject == null) continue;

                if (grabbableObject is Snowball snowball)
                {
                    nbSnowball += snowball.currentStackedItems;
                    player.DestroyItemInSlotAndSync(i);
                }
            }

            if (nbSnowball > 0)
                BuildSnowmanServerRpc(nbSnowball);
        }

        [ServerRpc(RequireOwnership = false)]
        public void BuildSnowmanServerRpc(int nbSnowball) => BuildSnowmanClientRpc(nbSnowball);

        [ClientRpc]
        public void BuildSnowmanClientRpc(int nbSnowball)
        {
            currentStackedSnowball += nbSnowball;
            if (currentStackedSnowball < ConfigManager.amountSnowballToBuild.Value)
                gameObject.transform.localScale = Constants.SNOWMAN_SCALE / ConfigManager.amountSnowballToBuild.Value * currentStackedSnowball;
            else
                gameObject.transform.localScale = Constants.SNOWMAN_SCALE;

            RefreshHoverTip();
        }

        [ServerRpc(RequireOwnership = false)]
        public void EnterSnowmanServerRpc(int playerId) => EnterSnowmanClientRpc(playerId);

        [ClientRpc]
        public void EnterSnowmanClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            player.DropAllHeldItems();

            snowmanTrigger.interactable = false;

            isPlayerHiding = true;
            hidingPlayer = player;

            transform.SetParent(player.transform);
            transform.position = player.transform.position;
            transform.rotation = player.transform.rotation;

            if (player == GameNetworkManager.Instance.localPlayerController)
            {
                camera.enabled = true;
                player.gameplayCamera = camera;

                HUDManager.Instance.ChangeControlTip(0, "Exit Snowman : [Q]", clearAllOther: true);

                SPUtilities.SetUntargetable(player);

                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).Disable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Jump", false).Disable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Crouch", false).Disable();
            }
        }

        public void ExitSnowman()
            => ExitSnowmanServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);

        [ServerRpc(RequireOwnership = false)]
        public void ExitSnowmanServerRpc(int playerId) => ExitSnowmanClientRpc(playerId);

        [ClientRpc]
        public void ExitSnowmanClientRpc(int playerId)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();

            transform.SetParent(null);

            if (player == GameNetworkManager.Instance.localPlayerController)
            {
                camera.enabled = false;
                player.gameplayCamera = playerCamera;

                HUDManager.Instance.ClearControlTips();

                SPUtilities.StartTargetable(GameNetworkManager.Instance.localPlayerController, ConfigManager.snowmanSlowdownDuration.Value);

                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move", false).Enable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Jump", false).Enable();
                IngamePlayerSettings.Instance.playerInput.actions.FindAction("Crouch", false).Enable();
            }

            Destroy(gameObject);
        }

        public override void OnDestroy()
        {
            PlaySnowmanParticle();
            SPUtilities.PlaySnowPoof(transform.position);
            base.OnDestroy();
        }

        public void PlaySnowmanParticle()
        {
            GameObject particleObj = Instantiate(SnowPlaygrounds.snowmanParticle, transform.position + Vector3.up * 2.5f, Quaternion.identity);
            ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
            Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !isPlayerHiding) return;

            EnemyAICollisionDetect collisionDetect = other.GetComponent<EnemyAICollisionDetect>();
            if (collisionDetect != null)
                FreezeEnemyServerRpc(collisionDetect.mainScript.NetworkObject);
        }

        [ServerRpc(RequireOwnership = false)]
        public void FreezeEnemyServerRpc(NetworkObjectReference enemyObject) => FreezeEnemyClientRpc(enemyObject);

        [ClientRpc]
        public void FreezeEnemyClientRpc(NetworkObjectReference enemyObject)
        {
            if (enemyObject.TryGet(out NetworkObject networkObject))
            {
                EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
                if (enemy != null)
                {
                    if (hidingPlayer != null && hidingPlayer == GameNetworkManager.Instance.localPlayerController)
                        ExitSnowman();
                    SPUtilities.StartFreezeEnemy(enemy, ConfigManager.snowmanSlowdownDuration.Value, ConfigManager.snowmanSlowdownFactor.Value);
                }
            }
        }

        public void RefreshHoverTip()
        {
            if (currentStackedSnowball < ConfigManager.amountSnowballToBuild.Value)
                snowmanTrigger.hoverTip = $"Add snowball {currentStackedSnowball}/{ConfigManager.amountSnowballToBuild.Value} : [LMB]";
            else
                snowmanTrigger.hoverTip = "Enter : [LMB]";
        }
    }
}
