using Artisan.Shared.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using YAPYAP;

namespace Artisan.Features.Inventory
{
    [HarmonyPatch(typeof(UIInventory), "Update")]
    public static class UIInventoryExtendedHotkeysPatch
    {
        private static void Postfix(UIInventory __instance)
        {
            if (ArtisanMod.EnableExtendedInventorySlots == null ||
                !ArtisanMod.EnableExtendedInventorySlots.Value)
                return;

            PawnInventory playerInventory = HarmonyUtil.GetFieldValue<PawnInventory>(
                __instance,
                "_playerInventory"
            );

            if (playerInventory == null)
                return;

            int maxSlots = InventorySlotLimitPatch.GetMaxInventorySlots();

            for (int slotIndex = 3; slotIndex < maxSlots; slotIndex++)
            {
                if (!ArtisanInventoryUpgradeService.IsSlotUnlocked(playerInventory, slotIndex))
                    continue;

                InputAction action = ArtisanInventoryHotkeyManager.GetSlotAction(slotIndex);

                if (action == null || !action.WasPressedThisFrame())
                    continue;

                playerInventory.CmdSelectSlotWithMainHand(slotIndex);
                return;
            }
        }
    }
}