using HarmonyLib;
using Mirror;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch]
    public static class ArtisanInventoryLockedSlotPatch
    {
        private static bool HasAvailablePickupSpace(PawnInventory inventory)
        {
            if (inventory == null)
                return false;

            if (ArtisanInventoryUpgradeService.GetFirstUnlockedEmptySlot(inventory) >= 0)
                return true;

            PawnPropInteractions propInteractions =
                inventory.GetComponent<PawnPropInteractions>();

            return propInteractions != null &&
                   propInteractions.NetworkCurrentLeftHandNetworkProp == null;
        }

        [HarmonyPatch(typeof(PawnInventory), "SelectSlotWithMainHand")]
        [HarmonyPrefix]
        private static bool SelectSlotWithMainHandPrefix(PawnInventory __instance, int slotIndex)
        {
            return ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, slotIndex);
        }

        [HarmonyPatch(typeof(PawnInventory), "UserCode_CmdSelectSlotWithMainHand__Int32")]
        [HarmonyPrefix]
        private static bool CmdSelectSlotPrefix(PawnInventory __instance, int slotIndex)
        {
            return ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, slotIndex);
        }

        [HarmonyPatch(typeof(PawnInventory), "UserCode_CmdSwapSlotWithRightHand__Int32")]
        [HarmonyPrefix]
        private static bool CmdSwapSlotPrefix(PawnInventory __instance, int slotIndex)
        {
            return ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, slotIndex);
        }

        [HarmonyPatch(typeof(PawnInventory), "UserCode_CmdMoveItemInInventory__UInt32__Int32")]
        [HarmonyPrefix]
        private static bool CmdMoveItemPrefix(PawnInventory __instance, uint sourcePropNetId, int targetSlotIndex)
        {
            return ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, targetSlotIndex);
        }

        [HarmonyPatch(typeof(PawnInventory), "UserCode_CmdCycleSlot__Boolean")]
        [HarmonyPrefix]
        private static bool CmdCycleSlotPrefix(PawnInventory __instance, bool cycleForward)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return true;

            int unlockedSlotCount = ArtisanInventoryUpgradeService.GetUnlockedSlotCount(__instance);

            if (unlockedSlotCount <= 0)
                return false;

            int current = __instance.CurrentMainHandSlot;

            for (int i = 0; i < unlockedSlotCount; i++)
            {
                current = cycleForward ? current - 1 : current + 1;

                if (current < 0)
                    current = unlockedSlotCount - 1;
                else if (current >= unlockedSlotCount)
                    current = 0;

                if (ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, current))
                {
                    __instance.SelectSlotWithMainHand(current);
                    return false;
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(PawnInventory), "UserCode_CmdAttemptPickup__NetworkPuppetProp")]
        [HarmonyPrefix]
        private static bool CmdAttemptPickupPrefix(PawnInventory __instance, NetworkPuppetProp prop)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return true;

            int slot = __instance.CurrentMainHandSlot;

            if (ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, slot))
                return true;

            __instance.ServerTryPickup(prop, 0);
            return false;
        }

        [HarmonyPatch(typeof(UIInventory), "OnSlotDrop")]
        [HarmonyPrefix]
        private static bool OnSlotDropPrefix(UIInventory __instance, UIInventorySlot targetSlot)
        {
            PawnInventory inventory = HarmonyUtil.GetFieldValue<PawnInventory>(__instance, "_playerInventory");
            UIInventorySlot draggedSlot = HarmonyUtil.GetFieldValue<UIInventorySlot>(__instance, "_draggedSlot");

            if (inventory == null || targetSlot == null)
                return true;

            if (IsLockedInventorySlot(inventory, targetSlot) || IsLockedInventorySlot(inventory, draggedSlot))
            {
                __instance.StopDrag();
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(PawnInventory), "ServerTryPickup")]
        [HarmonyPrefix]
        private static bool ServerTryPickupPrefix(PawnInventory __instance, NetworkPuppetProp prop, int preferredSlotIndex, ref bool __result)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return true;

            if (prop == null || !prop.CanPickedUpBy(__instance.netIdentity))
            {
                __result = false;
                return false;
            }

            if (preferredSlotIndex == -1)
                preferredSlotIndex = __instance.CurrentMainHandSlot;

            if (ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, preferredSlotIndex) &&
                __instance.ServerTryPickupInventorySlotOnly(prop, preferredSlotIndex))
            {
                __result = true;
                return false;
            }

            PawnPropInteractions propInteractions =
                HarmonyUtil.GetFieldValue<PawnPropInteractions>(__instance, "propInteractions");

            if (propInteractions != null && propInteractions.NetworkCurrentLeftHandNetworkProp == null)
            {
                prop.ServerHandleHeld(propInteractions, false);
                __result = true;
                return false;
            }

            int fallbackSlot = ArtisanInventoryUpgradeService.GetFirstUnlockedEmptySlot(__instance);

            if (fallbackSlot < 0)
            {
                __result = false;
                return false;
            }

            __result = __instance.ServerTryPickupInventorySlotOnly(prop, fallbackSlot);
            return false;
        }

        [HarmonyPatch(typeof(PawnInventory), "ServerTryPickupInventorySlotOnly")]
        [HarmonyPrefix]
        private static bool ServerTryPickupInventorySlotOnlyPrefix(PawnInventory __instance, int slotIndex, ref bool __result)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return true;

            if (ArtisanInventoryUpgradeService.IsSlotUnlocked(__instance, slotIndex))
                return true;

            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(NetworkPuppetProp), "CanInteract")]
        [HarmonyPostfix]
        private static void NetworkPuppetPropCanInteractPostfix(NetworkPuppetProp __instance, NetworkIdentity identity, ref bool __result)
        {
            if (!__result || !ArtisanInventoryUpgradeService.IsEnabled())
                return;

            PawnInventory inventory = identity == null ? null : identity.GetComponent<PawnInventory>();

            if (inventory == null)
                return;

            if (!HasAvailablePickupSpace(inventory))
                __result = false;
        }

        [HarmonyPatch(typeof(NetworkPuppetProp), "ShowTooltip")]
        [HarmonyPrefix]
        private static bool NetworkPuppetPropShowTooltipPrefix(NetworkPuppetProp __instance, NetworkIdentity identity)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return true;

            PawnInventory inventory = identity == null ? null : identity.GetComponent<PawnInventory>();

            if (inventory == null)
                return true;

            bool isFull = !HasAvailablePickupSpace(inventory);

            if (!isFull)
                return true;

            UIManager uiManager = UIManager.Instance;

            if (uiManager == null || uiManager.uiTooltip == null)
                return false;

            string text = __instance.DisplayName + " (" + uiManager.uiTooltip.InventoryFullStr + ")";

            uiManager.uiTooltip.ShowTooltip(
                text,
                InputTarget.Interact,
                false,
                false,
                __instance.IsGrabable ? uiManager.uiTooltip.DefaultGrabStr : string.Empty,
                InputTarget.Fire,
                __instance.IsGrabable,
                false,
                null,
                InputTarget.Use,
                true,
                false
            );

            return false;
        }

        [HarmonyPatch(typeof(PropIngredient), "CustomTooltipAction")]
        [HarmonyPostfix]
        private static void PropIngredientCustomTooltipActionPostfix(PropIngredient __instance, Interactable interactable, NetworkIdentity identity)
        {
            if (!ArtisanInventoryUpgradeService.IsEnabled())
                return;

            PawnInventory inventory = identity == null ? null : identity.GetComponent<PawnInventory>();

            if (inventory == null || HasAvailablePickupSpace(inventory))
                return;

            NetworkPuppetProp puppetProp = interactable as NetworkPuppetProp;

            if (puppetProp == null)
                puppetProp = __instance.GetComponent<NetworkPuppetProp>();

            if (puppetProp == null)
                return;

            UIManager uiManager = UIManager.Instance;

            if (uiManager == null || uiManager.uiTooltip == null)
                return;

            string text = puppetProp.DisplayName + " (" + uiManager.uiTooltip.InventoryFullStr + ")";

            uiManager.uiTooltip.ShowTooltip(
                text,
                InputTarget.Interact,
                false,
                false,
                null,
                InputTarget.Fire,
                puppetProp.IsGrabable,
                false,
                puppetProp.IsGrabable ? uiManager.uiTooltip.DefaultGrabStr : string.Empty,
                InputTarget.Fire,
                puppetProp.IsGrabable,
                false
            );
        }

        [HarmonyPatch(typeof(UIInventory), "OnDragHoverEnter")]
        [HarmonyPrefix]
        private static bool OnDragHoverEnterPrefix(UIInventory __instance, UIInventorySlot slot)
        {
            PawnInventory inventory = HarmonyUtil.GetFieldValue<PawnInventory>(__instance, "_playerInventory");

            return !IsLockedInventorySlot(inventory, slot);
        }

        [HarmonyPatch(typeof(UIInventorySlot), "OnBeginDrag")]
        [HarmonyPrefix]
        private static bool OnBeginDragPrefix(UIInventorySlot __instance)
        {
            UIInventory inventoryUi = HarmonyUtil.GetFieldValue<UIInventory>(__instance, "_inventory");
            PawnInventory inventory = HarmonyUtil.GetFieldValue<PawnInventory>(inventoryUi, "_playerInventory");

            return !IsLockedInventorySlot(inventory, __instance);
        }

        private static bool IsLockedInventorySlot(PawnInventory inventory, UIInventorySlot slot)
        {
            if (inventory == null || slot == null || slot.IsHandSlot)
                return false;

            return !ArtisanInventoryUpgradeService.IsSlotUnlocked(inventory, slot.SlotIndex);
        }
    }
}