using GameNetcodeStuff;
using HarmonyLib;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using LegaFusionCore.Utilities;
using SnowPlaygrounds.Behaviours.Items;
using SnowPlaygrounds.Behaviours.MapObjects;
using SnowPlaygrounds.Managers;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlaygrounds.Patches;

internal class PlayerControllerBPatch
{
    public static bool isTargetable = true;

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerLookInput))]
    [HarmonyPrefix]
    private static bool HandleSnowmanCamera(ref PlayerControllerB __instance)
    {
        if (LFCUtilities.ShouldBeLocalPlayer(__instance)
            && !__instance.quickMenuManager.isMenuOpen
            && __instance.gameObject.TryGetComponentInChildren(out Snowman snowman))
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
        if (!LFCUtilities.ShouldBeLocalPlayer(__instance)) return;
        if (__instance.gameObject.TryGetComponentInChildren(out Snowman snowman) && LFCUtilities.ShouldBeLocalPlayer(snowman?.hidingPlayer))
            snowman.ExitSnowman();
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DiscardHeldObject))]
    [HarmonyPrefix]
    private static bool DiscardHeldObject(ref PlayerControllerB __instance)
    {
        if (LFCUtilities.ShouldBeLocalPlayer(__instance) && __instance.currentlyHeldObjectServer is SnowBallItem snowBallItem && snowBallItem.currentStackedItems >= 1)
        {
            if (StartOfRound.Instance.shipHasLanded && __instance.isCrouching)
            {
                SnowPlaygroundsNetworkManager.Instance.SpawnSnowmanServerRpc((int)__instance.playerClientId, snowBallItem.currentStackedItems);
                LFCNetworkManager.Instance.DestroyObjectEveryoneRpc(snowBallItem.GetComponent<NetworkObject>());
                return false;
            }
            snowBallItem.ThrowSnowBallServerRpc(direction: Vector3.down, speed: 1f, angleDeg: 0f);
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItems))]
    [HarmonyPrefix]
    private static void DropAllHeldItems(ref PlayerControllerB __instance)
    {
        if (LFCUtilities.ShouldBeLocalPlayer(__instance))
        {
            for (int i = 0; i < __instance.ItemSlots.Length; i++)
            {
                GrabbableObject grabbableObject = __instance.ItemSlots[i];
                if (grabbableObject != null && grabbableObject is SnowBallItem snowBallItem && grabbableObject.IsSpawned)
                {
                    snowBallItem.ThrowSnowBallServerRpc(direction: Vector3.down, speed: 1f, angleDeg: 0f);
                    __instance.DestroyItemInSlot(i);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
    [HarmonyPrefix]
    private static bool SnowmanInteractTrigger(ref PlayerControllerB __instance)
    {
        if (__instance.isGrabbingObjectAnimation || __instance.inSpecialMenu || __instance.quickMenuManager.isMenuOpen) return true;

        __instance.interactRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
        if (Physics.Raycast(__instance.interactRay, out __instance.hit, __instance.grabDistance, __instance.interactableObjectsMask)
            && __instance.hit.collider.gameObject.layer != 8
            && __instance.hit.collider.gameObject.layer != 30
            && __instance.hit.collider.tag.Equals("InteractTrigger")
            && __instance.hit.collider.gameObject.TryGetComponentInParent(out Snowman snowman))
        {
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
            _ = stringBuilder.Replace("[E]", "[X]");
            _ = stringBuilder.Replace("[LMB]", "[X]");
            _ = stringBuilder.Replace("[RMB]", "[R-Trigger]");
            _ = stringBuilder.Replace("[F]", "[R-Shoulder]");
            _ = stringBuilder.Replace("[Z]", "[L-Shoulder]");
            player.cursorTip.text = stringBuilder.ToString();
            return;
        }
        player.cursorTip.text = player.cursorTip.text.Replace("[LMB]", "[E]");
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
    [HarmonyPostfix]
    private static void TeleportPlayer(PlayerControllerB __instance)
    {
        if (LFCUtilities.ShouldBeLocalPlayer(__instance))
            LFCStatRegistry.ClearModifiersWithTagPrefix(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone");
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    private static void KillPlayer(PlayerControllerB __instance)
    {
        if (LFCUtilities.ShouldBeLocalPlayer(__instance))
            LFCStatRegistry.ClearModifiersWithTagPrefix(LegaFusionCore.Constants.STAT_SPEED, $"{SnowPlaygrounds.modName}IceZone");
    }
}
