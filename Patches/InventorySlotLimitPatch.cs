using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch]
    public static class InventorySlotLimitPatch
    {
        private static readonly string[] TargetMethodNames =
        {
            "OnStartServer",
            "OnStartClient",
            "IsFull",
            "ServerTryAddItem",
            "ServerAddItemToSlot",
            "ServerRemoveFromSlot",
            "SelectSlotWithMainHand",
            "ServerSerializeToKvp",
            "ServerTryRestoreFromKvp",
            "UserCode_CmdCycleSlot__Boolean",
            "UserCode_CmdMoveItemInInventory__UInt32__Int32",
            "UserCode_CmdSwapSlotWithRightHand__Int32"
        };

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = typeof(PawnInventory);

            foreach (string methodName in TargetMethodNames)
            {
                MethodInfo method =
                    AccessTools.Method(type, methodName);

                if (method != null)
                    yield return method;
            }
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (!IsEnabled())
                {
                    yield return instruction;
                    continue;
                }

                if (IsLdcI4(instruction, 3))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(
                        typeof(InventorySlotLimitPatch),
                        nameof(GetMaxInventorySlots)
                    );
                }
                else if (IsLdcI4(instruction, 2))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(
                        typeof(InventorySlotLimitPatch),
                        nameof(GetMaxInventorySlotIndex)
                    );
                }

                yield return instruction;
            }
        }

        public static int GetMaxInventorySlots()
        {
            if (!IsEnabled())
                return 3;

            return Mathf.Clamp(ArtisanMod.MaxInventorySlots.Value, 3, 8);
        }

        public static int GetMaxInventorySlotIndex()
        {
            return GetMaxInventorySlots() - 1;
        }

        private static bool IsEnabled()
        {
            return ArtisanMod.EnableExtendedInventorySlots != null &&
                   ArtisanMod.EnableExtendedInventorySlots.Value;
        }

        private static bool IsLdcI4(CodeInstruction instruction, int value)
        {
            if (value == 2 && instruction.opcode == OpCodes.Ldc_I4_2)
                return true;

            if (value == 3 && instruction.opcode == OpCodes.Ldc_I4_3)
                return true;

            if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int intValue)
                return intValue == value;

            if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte sbyteValue)
                return sbyteValue == value;

            return false;
        }
    }

    [HarmonyPatch(typeof(UIInventory), "OnStartPawnAuthorityChanged")]
    public static class UIInventorySlotExpansionPatch
    {
        private static void Prefix(UIInventory __instance)
        {
            if (__instance == null ||
                ArtisanMod.EnableExtendedInventorySlots == null ||
                !ArtisanMod.EnableExtendedInventorySlots.Value)
                return;

            UIInventorySlot[] slots =
                HarmonyUtil.GetFieldValue<UIInventorySlot[]>(
                    __instance,
                    "inventorySlots"
                );

            int targetSlotCount = InventorySlotLimitPatch.GetMaxInventorySlots();

            if (slots == null || slots.Length == 0 || slots.Length >= targetSlotCount)
                return;

            UIInventorySlot templateSlot = slots[0];

            if (templateSlot == null)
                return;

            Transform parent = templateSlot.transform.parent;

            if (parent == null)
                return;

            UIInventorySlot[] expandedSlots =
                new UIInventorySlot[targetSlotCount];

            for (int i = 0; i < slots.Length; i++)
                expandedSlots[i] = slots[i];

            for (int i = slots.Length; i < targetSlotCount; i++)
            {
                GameObject slotObject =
                    UnityEngine.Object.Instantiate(
                        templateSlot.gameObject,
                        parent
                    );

                slotObject.name = "ArtisanInventorySlot_" + i;
                slotObject.SetActive(true);

                UIInventorySlot slot =
                    slotObject.GetComponent<UIInventorySlot>();

                if (slot == null)
                {
                    UnityEngine.Object.Destroy(slotObject);
                    continue;
                }

                LayoutElement layoutElement =
                    slotObject.GetComponent<LayoutElement>();

                if (layoutElement != null)
                    layoutElement.ignoreLayout = true;

                expandedSlots[i] = slot;
            }

            HarmonyUtil.SetFieldValue(
                __instance,
                "inventorySlots",
                expandedSlots
            );
        }
    }
}