using YAPYAP;
using Mirror;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using YAPYAP.Npc.Shopkeeper;

namespace Artisan.Patches
{
    public static class ArtisanInventoryUpgradeService
    {
        private const int VanillaSlotCount = 3;
        private const string UnlockedExtraSlotsKey = ".UNLOCKED_EXTRA_SLOTS";

        public static bool IsEnabled()
        {
            return ArtisanMod.EnableExtendedInventorySlots != null &&
                   ArtisanMod.EnableExtendedInventorySlots.Value &&
                   ArtisanMod.EnableInventorySlotUpgrades != null &&
                   ArtisanMod.EnableInventorySlotUpgrades.Value;
        }

        public static int GetMaxExtraSlots()
        {
            return Mathf.Max(0, InventorySlotLimitPatch.GetMaxInventorySlots() - VanillaSlotCount);
        }

        public static int GetUnlockedExtraSlots(PawnInventory inventory)
        {
            if (!IsEnabled())
                return GetMaxExtraSlots();

            Pawn pawn = GetPawn(inventory);

            if (pawn == null || string.IsNullOrEmpty(pawn.PlayerId))
                return 0;

            SaveManager save;

            if (!Service.Get(out save))
                return 0;

            int value;

            if (!save.TryGetInt(GetUnlockedExtraSlotsKey(pawn.PlayerId), out value))
                return 0;

            return Mathf.Clamp(value, 0, GetMaxExtraSlots());
        }

        public static int GetUnlockedSlotCount(PawnInventory inventory)
        {
            return VanillaSlotCount + GetUnlockedExtraSlots(inventory);
        }

        public static bool IsSlotUnlocked(PawnInventory inventory, int slotIndex)
        {
            if (slotIndex < VanillaSlotCount)
                return true;

            return slotIndex >= 0 && slotIndex < GetUnlockedSlotCount(inventory);
        }

        public static int GetNextUpgradePrice(PawnInventory inventory)
        {
            int unlockedExtraSlots = GetUnlockedExtraSlots(inventory);
            int basePrice = Mathf.Max(0, ArtisanMod.ExtraSlotBasePrice.Value);
            float multiplier = Mathf.Max(1f, ArtisanMod.ExtraSlotPriceMultiplier.Value);

            return Mathf.CeilToInt(basePrice * Mathf.Pow(multiplier, unlockedExtraSlots));
        }

        public static bool CanPurchaseNextUpgrade(PawnInventory inventory)
        {
            if (!IsEnabled())
                return false;

            if (!NetworkServer.active)
                return false;

            if (GameManager.Instance == null ||
                GameManager.Instance.CurrentGameState != GameManager.GameState.Lobby)
                return false;

            Pawn pawn = GetPawn(inventory);

            if (pawn == null || string.IsNullOrEmpty(pawn.PlayerId))
                return false;

            if (GetUnlockedExtraSlots(inventory) >= GetMaxExtraSlots())
                return false;

            return GameManager.Instance.Gold >= GetNextUpgradePrice(inventory);
        }

        public static bool TryPurchaseNextUpgrade(PawnInventory inventory)
        {
            if (!CanPurchaseNextUpgrade(inventory))
                return false;

            Pawn pawn = GetPawn(inventory);
            SaveManager save;

            if (pawn == null || !Service.Get(out save))
                return false;

            int price = GetNextUpgradePrice(inventory);
            int unlockedExtraSlots = GetUnlockedExtraSlots(inventory) + 1;

            GameManager.Instance.ModifyGold(-price);
            save.SetInt(GetUnlockedExtraSlotsKey(pawn.PlayerId), unlockedExtraSlots);

            inventory.ServerPersistInventory();

            return true;
        }

        public static int GetFirstUnlockedEmptySlot(PawnInventory inventory)
        {
            if (inventory == null)
                return -1;

            int unlockedSlotCount = GetUnlockedSlotCount(inventory);

            for (int i = 0; i < unlockedSlotCount; i++)
            {
                if (!IsSlotUnlocked(inventory, i))
                    continue;

                if (i >= inventory.Items.Count || inventory.Items[i].IsEmpty)
                    return i;
            }

            return -1;
        }

        private static Pawn GetPawn(PawnInventory inventory)
        {
            return inventory == null ? null : inventory.GetComponent<Pawn>();
        }

        private static string GetUnlockedExtraSlotsKey(string playerId)
        {
            return "PLAYER." + playerId + ".INV" + UnlockedExtraSlotsKey;
        }
    }
}