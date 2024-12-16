using GameNetcodeStuff;
using SnowPlaygrounds.Managers;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Behaviours.Items
{
    public class Snowball : PhysicsProp
    {
        public int currentStackedItems = ConfigManager.snowballAmount.Value;
        public Rigidbody rigidbody;
        public AudioSource snowPoof;

        public bool isThrown = false;
        public PlayerControllerB throwingPlayer;

        public override void Start()
        {
            base.Start();

            if (rigidbody == null)
                rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
                SnowPlaygrounds.mls.LogError("Rigidbody is not assigned and could not be found in children.");

            if (snowPoof == null)
                snowPoof = GetComponent<AudioSource>();
            if (snowPoof == null)
                SnowPlaygrounds.mls.LogError("SnowPoof is not assigned and could not be found.");
        }

        public override void Update()
        {
            if (!isThrown)
                base.Update(); ;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown && playerHeldBy != null)
                ThrowSnowballServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropSnowballServerRpc(int playerId) => DropSnowballClientRpc(playerId, InstantiateSnowballToThrow());

        [ClientRpc]
        public void DropSnowballClientRpc(int playerId, NetworkObjectReference obj)
        {
            Snowball snowball = InitializeSnowballToThrow(obj);
            if (snowball != null)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                if (player.isInElevator)
                    snowball.transform.SetParent(player.playersManager.elevatorTransform, worldPositionStays: true);

                if ((bool)snowball.transform.parent)
                    snowball.startFallingPosition = snowball.transform.parent.InverseTransformPoint(snowball.startFallingPosition);
                snowball.FallToGround();

                snowball.StartCoroutine(snowball.DetectGroundAndWalls());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ThrowSnowballServerRpc() => ThrowSnowballClientRpc(InstantiateSnowballToThrow());

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

        public Snowball InitializeSnowballToThrow(NetworkObjectReference obj)
        {
            Snowball snowball = null;
            if (obj.TryGet(out var networkObject))
            {
                snowball = networkObject.gameObject.GetComponentInChildren<GrabbableObject>() as Snowball;
                snowball.Start();
                snowball.isThrown = true;
                snowball.throwingPlayer = playerHeldBy;
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
                    ApplyDecalServerRpc(hitDown.point, hitDown.normal);
                    break;
                }
                yield return null;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyDecalServerRpc(Vector3 point, Vector3 normal) => ApplyDecalClientRpc(point, normal);

        [ClientRpc]
        public void ApplyDecalClientRpc(Vector3 point, Vector3 normal)
        {
            GameObject decal = Instantiate(SnowPlaygrounds.snowballDecal);
            decal.transform.position = point + normal * 0.01f;
            decal.transform.forward = normal;

            SnowballImpact(decal.transform.position, Quaternion.LookRotation(normal));

            StartCoroutine(DestroyCoroutine());
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
        }

        private bool HandleEnemyHit(Collider other)
        {
            EnemyAICollisionDetect collisionDetect = other.GetComponent<EnemyAICollisionDetect>();
            if (collisionDetect != null)
            {
                FreezeEnemyServerRpc(collisionDetect.mainScript.NetworkObject, (int)throwingPlayer.playerClientId, other.ClosestPoint(transform.position));
                FinalizeHit();
                return true;
            }
            return false;
        }

        private bool HandlePlayerHit(Collider other)
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player != null && player != throwingPlayer)
            {
                Vector3 force = (player.transform.position - throwingPlayer.transform.position).normalized * ConfigManager.snowballPushForce.Value;
                HitPlayerServerRpc((int)player.playerClientId, force, other.ClosestPoint(transform.position));
                FinalizeHit();
                return true;
            }
            return false;
        }

        private void FinalizeHit()
        {
            isThrown = false;
            DestroyObjectServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void FreezeEnemyServerRpc(NetworkObjectReference enemyObject, int playerId, Vector3 position) => FreezeEnemyClientRpc(enemyObject, playerId, position);

        [ClientRpc]
        public void FreezeEnemyClientRpc(NetworkObjectReference enemyObject, int playerId, Vector3 position)
        {
            if (enemyObject.TryGet(out NetworkObject networkObject))
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
                SnowballImpact(position, player.transform.rotation);

                EnemyAI enemy = networkObject.gameObject.GetComponentInChildren<EnemyAI>();
                if (enemy != null)
                    SPUtilities.StartFreezeEnemy(enemy, ConfigManager.snowballSlowdownDuration.Value, ConfigManager.snowballSlowdownFactor.Value);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void HitPlayerServerRpc(int playerId, Vector3 force, Vector3 position) => HitPlayerClientRpc(playerId, force, position);

        [ClientRpc]
        public void HitPlayerClientRpc(int playerId, Vector3 force, Vector3 position)
        {
            PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
            SnowballImpact(position, player.transform.rotation);

            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                player.thisController.Move(force);
                HUDManager.Instance.flashFilter = Mathf.Min(1f, HUDManager.Instance.flashFilter + 0.4f);
            }
        }

        public void SnowballImpact(Vector3 position, Quaternion rotation)
        {
            snowPoof.Play();

            GameObject particleObj = Instantiate(SnowPlaygrounds.snowballParticle, position, rotation);
            ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
            Destroy(particleObj, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DestroyObjectServerRpc() => DestroyObjectClientRpc();

        [ClientRpc]
        public void DestroyObjectClientRpc() => DestroyObjectInHand(throwingPlayer);
    }
}
