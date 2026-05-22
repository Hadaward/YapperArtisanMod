using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;
using YAPYAP;

namespace Artisan.Patches
{
    [HarmonyPatch]
    public static class InventoryPersistencePatch
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(
                typeof(PawnInventory),
                "ServerSerializeToKvp"
            );

            yield return AccessTools.Method(
                typeof(PawnInventory),
                "ServerTryRestoreFromKvp"
            );
        }

        private static void Postfix(PawnInventory __instance, SaveManager save, string playerId, MethodBase __originalMethod)
        {
            if (!IsEnabled() || __instance == null || save == null || string.IsNullOrEmpty(playerId))
                return;

            try
            {
                if (__originalMethod.Name == "ServerSerializeToKvp")
                {
                    SerializeExtendedSlots(__instance, save, playerId);
                    return;
                }

                if (__originalMethod.Name == "ServerTryRestoreFromKvp")
                    RestoreExtendedSlots(__instance, save, playerId);
            }
            catch (Exception ex)
            {
                ArtisanMod.Logger.LogWarning("[InventoryPersistence] Failed: " + ex.Message);
            }
        }

        private static void SerializeExtendedSlots(PawnInventory inventory, SaveManager save, string playerId)
        {
            string keyPrefix = GetKeyPrefix(playerId);
            int maxSlots = InventorySlotLimitPatch.GetMaxInventorySlots();

            save.SetInt(keyPrefix + ".SLOT_COUNT", maxSlots);
            save.SetInt(
                keyPrefix + ".MAIN",
                Mathf.Clamp(inventory.CurrentMainHandSlot, 0, maxSlots - 1)
            );

            MethodInfo serializeProp =
                AccessTools.Method(typeof(PawnInventory), "SerializeProp");

            if (serializeProp == null)
                return;

            for (int i = 0; i < maxSlots; i++)
            {
                NetworkPuppetProp prop = null;

                if (i < inventory.Items.Count && !inventory.Items[i].IsEmpty)
                    prop = inventory.Items[i].PropInstance;

                serializeProp.Invoke(
                    inventory,
                    new object[]
                    {
                        save,
                        keyPrefix + ".S" + i,
                        prop
                    }
                );
            }
        }

        private static void RestoreExtendedSlots(PawnInventory inventory, SaveManager save, string playerId)
        {
            string keyPrefix = GetKeyPrefix(playerId);
            int maxSlots = InventorySlotLimitPatch.GetMaxInventorySlots();

            MethodInfo tryRestoreProp =
                AccessTools.Method(typeof(PawnInventory), "TryRestoreProp");

            MethodInfo serverAddItemToSlot =
                AccessTools.Method(typeof(PawnInventory), "ServerAddItemToSlot");

            if (tryRestoreProp == null || serverAddItemToSlot == null)
                return;

            for (int i = 3; i < maxSlots; i++)
            {
                if (i < inventory.Items.Count && !inventory.Items[i].IsEmpty)
                    continue;

                object[] args =
                {
                    save,
                    keyPrefix + ".S" + i,
                    playerId,
                    null
                };

                bool restored =
                    (bool)tryRestoreProp.Invoke(inventory, args);

                if (!restored || args[3] == null)
                    continue;

                NetworkPuppetProp prop = args[3] as NetworkPuppetProp;

                if (prop == null)
                    continue;

                InventoryItem item = new InventoryItem(prop);

                bool added =
                    (bool)serverAddItemToSlot.Invoke(
                        inventory,
                        new object[]
                        {
                            i,
                            item
                        }
                    );

                if (!added)
                    continue;

                PawnPropInteractions propInteractions =
                    AccessTools.Field(typeof(PawnInventory), "propInteractions")
                        ?.GetValue(inventory) as PawnPropInteractions;

                SetPropInInventory(prop, inventory);
            }

            RestoreSelectedMainSlot(inventory, save, keyPrefix, maxSlots);
            NormalizeRestoredInventoryProps(inventory);
        }

        private static void SetPropInInventory(NetworkPuppetProp prop, PawnInventory inventory)
        {
            if (prop == null || inventory == null)
                return;

            PawnPropInteractions propInteractions =
                AccessTools.Field(typeof(PawnInventory), "propInteractions")
                    ?.GetValue(inventory) as PawnPropInteractions;

            if (propInteractions != null)
            {
                prop.ServerSetInInventory(propInteractions);
                return;
            }

            prop.CurrentState =
                new NetworkPuppetProp.PropStateData(
                    PropState.InInventory,
                    null,
                    true
                );
        }

        private static void NormalizeRestoredInventoryProps(PawnInventory inventory)
        {
            if (inventory == null)
                return;

            int currentSlot = inventory.CurrentMainHandSlot;

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                if (i == currentSlot)
                    continue;

                InventoryItem item = inventory.Items[i];

                if (item.IsEmpty || item.PropInstance == null)
                    continue;

                SetPropInInventory(item.PropInstance, inventory);
            }

            if (currentSlot >= 0 &&
                currentSlot < inventory.Items.Count &&
                !inventory.Items[currentSlot].IsEmpty)
            {
                inventory.SelectSlotWithMainHand(currentSlot);
            }
        }

        private static void RestoreSelectedMainSlot(PawnInventory inventory, SaveManager save, string keyPrefix, int maxSlots)
        {
            int savedMainSlot;

            if (!save.TryGetInt(keyPrefix + ".MAIN", out savedMainSlot))
                return;

            savedMainSlot = Mathf.Clamp(savedMainSlot, 0, maxSlots - 1);

            if (savedMainSlot >= inventory.Items.Count)
                return;

            if (inventory.Items[savedMainSlot].IsEmpty)
                return;

            inventory.SelectSlotWithMainHand(savedMainSlot);
        }

        private static string GetKeyPrefix(string playerId)
        {
            return "PLAYER." + playerId + ".INV";
        }

        private static bool IsEnabled()
        {
            return ArtisanMod.EnableExtendedInventorySlots != null &&
                   ArtisanMod.EnableExtendedInventorySlots.Value;
        }
    }
}