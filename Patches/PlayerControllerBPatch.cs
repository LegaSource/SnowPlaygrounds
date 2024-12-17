using GameNetcodeStuff;
using HarmonyLib;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
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
                if (__instance.isCrouching)
                    SnowPlaygroundsNetworkManager.Instance.SpawnSnowmanServerRpc(snowball.GetComponent<NetworkObject>(), __instance.transform.position + Vector3.up * 1.5f);
                else
                    snowball.DropSnowballServerRpc((int)__instance.playerClientId);
                return false;
            }
            return true;
        }
    }
}
