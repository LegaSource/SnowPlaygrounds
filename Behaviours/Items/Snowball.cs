using GameNetcodeStuff;
using SnowPlaygrounds.Behaviours.Enemies;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items
{
    public class Snowball : PhysicsProp
    {
        public bool isInitialized = false;
        public int currentStackedItems = 0;
        public Rigidbody rigidbody;

        public bool isThrown = false;
        public PlayerControllerB throwingPlayer;

        public Coroutine throwCooldownCoroutine;

        public override void Start()
        {
            if (isInitialized) return;
            isInitialized = true;

            base.Start();

            if (rigidbody == null)
                rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
                SnowPlaygrounds.mls.LogError("Rigidbody is not assigned and could not be found.");

            currentStackedItems = ConfigManager.snowballAmount.Value;
        }

        public override void Update()
        {
            if (!isThrown)
                base.Update();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown && playerHeldBy != null)
                throwCooldownCoroutine ??= StartCoroutine(ThrowCooldownCoroutine());
        }

        private IEnumerator ThrowCooldownCoroutine()
        {
            ThrowSnowballServerRpc();
            yield return new WaitForSeconds(ConfigManager.snowballThrowCooldown.Value);
            throwCooldownCoroutine = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropSnowballServerRpc(int playerId)
            => DropSnowballClientRpc(playerId, InstantiateSnowballToThrow());

        [ClientRpc]
        public void DropSnowballClientRpc(int playerId, NetworkObjectReference obj)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            Snowball snowball = InitializeSnowballToThrow(obj, player);
            if (snowball != null)
            {
                if (player.isInElevator)
                    snowball.transform.SetParent(player.playersManager.elevatorTransform, worldPositionStays: true);

                if ((bool)snowball.transform.parent)
                    snowball.startFallingPosition = snowball.transform.parent.InverseTransformPoint(snowball.startFallingPosition);
                snowball.FallToGround();

                snowball.StartCoroutine(snowball.DetectGroundAndWalls());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ThrowSnowballServerRpc()
            => ThrowSnowballClientRpc(InstantiateSnowballToThrow());

        [ClientRpc]
        public void ThrowSnowballClientRpc(NetworkObjectReference obj)
        {
            Snowball snowball = InitializeSnowballToThrow(obj);
            if (snowball != null)
            {
                Vector3 throwDirection = snowball.throwingPlayer.gameplayCamera.transform.forward;
                // L'angle ne doit pas être trop bas
                float minY = -0.07f;
                if (throwDirection.y < minY)
                    throwDirection = new Vector3(throwDirection.x, minY, throwDirection.z).normalized;
                Vector3 horizontalVelocity = throwDirection * 30f; // Vitesse horizontale
                Vector3 verticalVelocity = new Vector3(0, 3f, 0); // Vitesse verticale pour créer l'arc

                // Réinitialisation de la vélocité avant d'appliquer la nouvelle force
                snowball.rigidbody.velocity = Vector3.zero;
                snowball.rigidbody.AddForce(horizontalVelocity, ForceMode.VelocityChange);
                snowball.rigidbody.AddForce(verticalVelocity, ForceMode.VelocityChange);

                snowball.StartCoroutine(snowball.DetectGroundAndWalls());
            }
        }

        public NetworkObject InstantiateSnowballToThrow()
        {
            NetworkObject networkObject;
            if (currentStackedItems > 1)
            {
                GameObject gameObject = Instantiate(SnowPlaygrounds.snowballObj, transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
                networkObject = gameObject.GetComponent<NetworkObject>();
                networkObject.Spawn();
                currentStackedItems--;
            }
            else
            {
                networkObject = GetComponent<NetworkObject>();
            }
            return networkObject;
        }

        public Snowball InitializeSnowballToThrow(NetworkObjectReference obj, PlayerControllerB player = null)
        {
            Snowball snowball = null;
            if (obj.TryGet(out var networkObject))
            {
                snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as Snowball;
                snowball.Start();
                snowball.isThrown = true;
                snowball.throwingPlayer = player ?? playerHeldBy;
                if (snowball.isHeld)
                    snowball.throwingPlayer.DiscardHeldObject();
                // Fixer la position de la boule de neige
                snowball.transform.position = snowball.throwingPlayer.transform.position + Vector3.up * 1.5f;
                snowball.startFallingPosition = snowball.transform.position;
            }
            return snowball;
        }

        public IEnumerator DetectGroundAndWalls()
        {
            while (isThrown)
            {
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitDown, 0.25f, 605030721, QueryTriggerInteraction.Collide))
                {
                    SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
                    StartCoroutine(DestroyCoroutine());
                    break;
                }
                yield return null;
            }
        }

        public IEnumerator DestroyCoroutine()
        {
            yield return new WaitForSeconds(1f);
            if (!deactivated)
                DestroyObjectInHand(throwingPlayer);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !isThrown || throwingPlayer == null) return;
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (localPlayer != throwingPlayer) return;

            if (HandleEnemyHit(other)) return;
            if (HandlePlayerHit(other)) return;
            if (HandleSnowmanHit(other)) return;
        }

        private bool HandleEnemyHit(Collider other)
        {
            EnemyAICollisionDetect enemyCollision = other.GetComponent<EnemyAICollisionDetect>();
            if (enemyCollision == null) return false;

            FreezeEnemyServerRpc(enemyCollision.mainScript.NetworkObject, (int)throwingPlayer.playerClientId, other.ClosestPoint(transform.position));

            FinalizeHit();
            return true;
        }

        private bool HandlePlayerHit(Collider other)
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player == null) return false;
            if (player == throwingPlayer) return false;

            Vector3 force = (player.transform.position - throwingPlayer.transform.position).normalized * ConfigManager.snowballPushForce.Value;
            HitPlayerServerRpc((int)player.playerClientId, force, other.ClosestPoint(transform.position));
            FinalizeHit();
            return true;
        }

        private bool HandleSnowmanHit(Collider other)
        {
            Snowman snowman = other.GetComponent<Snowman>();
            if (snowman == null) return false;

            if (snowman.isEnemyHiding)
                snowman.SpawnFrostbiteServerRpc();
            else if (snowman.hidingPlayer != null)
                snowman.ExitSnowmanClientRpc((int)snowman.hidingPlayer.playerClientId);
            else
                SnowPlaygroundsNetworkManager.Instance.DestroySnowmanClientRpc(snowman.GetComponent<NetworkObject>());
            FinalizeHit();
            return true;
        }

        private void FinalizeHit()
        {
            isThrown = false;
            DestroyObjectServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void FreezeEnemyServerRpc(NetworkObjectReference enemyObject, int playerId, Vector3 position)
            => FreezeEnemyClientRpc(enemyObject, playerId, position);

        [ClientRpc]
        public void FreezeEnemyClientRpc(NetworkObjectReference enemyObject, int playerId, Vector3 position)
        {
            if (enemyObject.TryGet(out NetworkObject networkObject))
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                SPUtilities.SnowballImpact(position, player.transform.rotation);

                EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
                if (enemy != null)
                {
                    if (enemy is FrostbiteAI frostbite)
                        frostbite.HitFrostbite();
                    else
                        SPUtilities.StartFreezeEnemy(enemy, ConfigManager.snowballSlowdownDuration.Value, ConfigManager.snowballSlowdownFactor.Value);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HitPlayerServerRpc(int playerId, Vector3 force, Vector3 position)
            => HitPlayerClientRpc(playerId, force, position);

        [ClientRpc]
        public void HitPlayerClientRpc(int playerId, Vector3 force, Vector3 position)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            SPUtilities.SnowballImpact(position, player.transform.rotation);

            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                player.thisController.Move(force);
                HUDManager.Instance.flashFilter = Mathf.Min(1f, HUDManager.Instance.flashFilter + 0.4f);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyObjectServerRpc()
            => DestroyObjectClientRpc();

        [ClientRpc]
        public void DestroyObjectClientRpc()
            => DestroyObjectInHand(throwingPlayer);
    }
}
