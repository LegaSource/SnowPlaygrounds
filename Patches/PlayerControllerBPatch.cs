using GameNetcodeStuff;
using HarmonyLib;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches
{
    internal class PlayerControllerBPatch
    {
        public static int cameraCollideLayerMask = (1 << LayerMask.NameToLayer("Room")) | (1 << LayerMask.NameToLayer("PlaceableShipObject")) | (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("MiscLevelGeometry"));
        public static float targetCameraDistance = 3f;

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerLookInput))]
        [HarmonyPrefix]
        private static bool HandleSnowmanCamera(ref PlayerControllerB __instance)
        {
            if (__instance != GameNetworkManager.Instance.localPlayerController) return true;

            Snowman snowman = __instance.GetComponentInChildren<Snowman>();
            if (snowman == null) return true;

            if (!__instance.quickMenuManager.isMenuOpen)
            {
                Vector2 lookInput = __instance.playerActions.Movement.Look.ReadValue<Vector2>() * IngamePlayerSettings.Instance.settings.lookSensitivity * 0.008f;
                snowman.cameraPivot.Rotate(new Vector3(0f, lookInput.x, 0f));

                // Rotation verticale avec clamping
                float verticalAngle = snowman.cameraPivot.localEulerAngles.x - lookInput.y;
                verticalAngle = (verticalAngle > 180f) ? (verticalAngle - 360f) : verticalAngle;
                verticalAngle = Mathf.Clamp(verticalAngle, -45f, 45f);
                snowman.cameraPivot.localEulerAngles = new Vector3(verticalAngle, snowman.cameraPivot.localEulerAngles.y, 0f);

                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ItemSecondaryUse_performed))]
        [HarmonyPostfix]
        private static void SecondaryUsePerformed(ref PlayerControllerB __instance)
        {
            if (__instance == GameNetworkManager.Instance.localPlayerController)
            {
                Snowman snowman = __instance.GetComponentInChildren<Snowman>();
                if (snowman != null && snowman.hidingPlayer != null && snowman.hidingPlayer == __instance)
                    snowman.ExitSnowman();
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DiscardHeldObject))]
        [HarmonyPrefix]
        private static bool PreDropObject(ref PlayerControllerB __instance)
        {
            if (__instance.currentlyHeldObjectServer is Snowball snowball && !snowball.isThrown)
            {
                if (StartOfRound.Instance.shipHasLanded && __instance.isCrouching)
                {
                    SnowPlaygroundsNetworkManager.Instance.SpawnSnowmanServerRpc(
                        (int)__instance.playerClientId,
                        snowball.GetComponent<NetworkObject>(),
                        __instance.transform.position + Vector3.up * 1.5f,
                        __instance.gameplayCamera.transform.rotation);
                }
                else
                {
                    snowball.DropSnowballServerRpc((int)__instance.playerClientId);
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItems))]
        [HarmonyPrefix]
        private static void PreDropAllObjects(ref PlayerControllerB __instance)
        {
            bool isSnowballThrown = false;

            for (int i = 0; i < __instance.ItemSlots.Length; i++)
            {
                GrabbableObject grabbableObject = __instance.ItemSlots[i];
                if (grabbableObject != null && grabbableObject is Snowball)
                {
                    __instance.DestroyItemInSlot(i);
                    isSnowballThrown = true;
                }
            }

            if (isSnowballThrown && Physics.Raycast(__instance.transform.position + Vector3.up, Vector3.down, out RaycastHit hitDown, 2f, 605030721, QueryTriggerInteraction.Collide))
                SPUtilities.ApplyDecal(hitDown.point, hitDown.normal);
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
        [HarmonyPrefix]
        private static bool SnowmanInteractTrigger(ref PlayerControllerB __instance)
        {
            if (__instance.isGrabbingObjectAnimation || __instance.inSpecialMenu || __instance.quickMenuManager.isMenuOpen) return true;

            __instance.interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (Physics.Raycast(__instance.interactRay, out __instance.hit, __instance.grabDistance, __instance.interactableObjectsMask) && __instance.hit.collider.gameObject.layer != 8 && __instance.hit.collider.gameObject.layer != 30)
            {
                if (!__instance.hit.collider.tag.Equals("InteractTrigger")) return true;

                Snowman snowman = __instance.hit.collider.gameObject.GetComponentInParent<Snowman>();
                if (snowman == null) return true;

                __instance.hoveringOverTrigger = snowman.snowmanTrigger;
                if (!__instance.isHoldingInteract)
                {
                    __instance.cursorIcon.enabled = true;
                    __instance.cursorIcon.sprite = snowman.snowmanTrigger.hoverIcon;
                    __instance.cursorTip.text = snowman.snowmanTrigger.hoverTip;
                }

                FormatCursorTip(__instance);
                return false;
            }
            return true;
        }

        private static void FormatCursorTip(PlayerControllerB player)
        {
            if (StartOfRound.Instance.localPlayerUsingController)
            {
                StringBuilder stringBuilder = new StringBuilder(player.cursorTip.text);
                stringBuilder.Replace("[E]", "[X]");
                stringBuilder.Replace("[LMB]", "[X]");
                stringBuilder.Replace("[RMB]", "[R-Trigger]");
                stringBuilder.Replace("[F]", "[R-Shoulder]");
                stringBuilder.Replace("[Z]", "[L-Shoulder]");
                player.cursorTip.text = stringBuilder.ToString();
            }
            else
            {
                player.cursorTip.text = player.cursorTip.text.Replace("[LMB]", "[E]");
            }
        }
    }
}
