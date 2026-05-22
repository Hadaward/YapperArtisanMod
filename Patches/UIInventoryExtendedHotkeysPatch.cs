using HarmonyLib;
using UnityEngine;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch(typeof(UIInventory), "Update")]
    public static class UIInventoryExtendedHotkeysPatch
    {
        private static readonly KeyCode[] SlotKeys =
        {
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8
        };

        private static void Postfix(UIInventory __instance)
        {
            if (ArtisanMod.EnableExtendedInventorySlots == null ||
                !ArtisanMod.EnableExtendedInventorySlots.Value)
                return;

            PawnInventory playerInventory =
                HarmonyUtil.GetFieldValue<PawnInventory>(
                    __instance,
                    "_playerInventory"
                );

            if (playerInventory == null)
                return;

            int maxSlots = InventorySlotLimitPatch.GetMaxInventorySlots();

            for (int i = 3; i < maxSlots && i < 8; i++)
            {
                if (!UnityEngine.Input.GetKeyDown(SlotKeys[i - 3]))
                    continue;

                playerInventory.CmdSelectSlotWithMainHand(i);
                return;
            }
        }
    }
}