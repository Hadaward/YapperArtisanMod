using Artisan.Features.Inventory;
using Artisan.Localization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace Artisan
{
    [BepInPlugin("gamedroit.artisan", "Artisan", "1.0.3")]
    public sealed class ArtisanMod : BaseUnityPlugin
    {
        internal new static ManualLogSource Logger { get; private set; }

        private Harmony harmony;

        public static ConfigEntry<bool> EnableTeleBlastDamagePatch;
        public static ConfigEntry<bool> MakeTeleBlastDamageOtherPlayers;
        public static ConfigEntry<int> TeleBlastSpellDamage;

        public static ConfigEntry<bool> MakeAeroDamageOtherPlayers;
        public static ConfigEntry<bool> EnableAeroDamagePatch;
        public static ConfigEntry<int> AeroSpellDamage;

        public static ConfigEntry<bool> EnableExtendedInventorySlots;
        public static ConfigEntry<int> MaxInventorySlots;

        public static ConfigEntry<bool> EnableInventorySlotUpgrades;
        public static ConfigEntry<int> ExtraSlotBasePrice;
        public static ConfigEntry<float> ExtraSlotPriceMultiplier;

        public static ConfigEntry<bool> EnableShowMonstersHealthBar;
        public static ConfigEntry<bool> EnableShowMonsterDamageIndicator;
        public static ConfigEntry<float> MonsterHealthBarExtraHeightOffset;

        public void Awake()
        {
            Logger = base.Logger;

            EnableAeroDamagePatch = Config.Bind("Aero", "Enable Aero Damage Patch", true, "Enable or disable the damage patch for Aero spell.");
            MakeAeroDamageOtherPlayers = Config.Bind("Aero", "Make Aero Damage Other Players", false, "Allow Aero spell to damage other players when the damage patch is enabled.");
            AeroSpellDamage = Config.Bind("Aero", "Aero Spell Damage", 2, "Amount of damage the Aero spell will deal when the damage patch is enabled.");

            EnableTeleBlastDamagePatch = Config.Bind("TeleBlast", "Enable TeleBlast Damage Patch", true, "Enable or disable the damage patch for TeleBlast spell.");
            MakeTeleBlastDamageOtherPlayers = Config.Bind("TeleBlast", "Make TeleBlast Damage Other Players", false, "Allow TeleBlast spell to damage other players when the damage patch is enabled.");
            TeleBlastSpellDamage = Config.Bind("TeleBlast", "TeleBlast Spell Damage", 5, "Amount of damage the TeleBlast spell will deal when the damage patch is enabled.");

            EnableExtendedInventorySlots = Config.Bind("Inventory", "Enable Extended Inventory Slots", true, "Enable or disable increasing the inventory from 3 to 6 slots.");
            MaxInventorySlots = Config.Bind("Inventory", "Max Inventory Slots", 6, "Maximum inventory slots. Vanilla is 3. Recommended value is 6.");

            EnableInventorySlotUpgrades = Config.Bind("Inventory", "Enable Inventory Slot Upgrades", true, "Enable or disable the ability to upgrade inventory slots. When disabled, all extra slots will be available from the start without needing to purchase them.");
            ExtraSlotBasePrice = Config.Bind("Inventory", "Extra Slot Base Price", 40, "Base price for the first extra inventory slot. Each subsequent slot will cost more based on the multiplier.");
            ExtraSlotPriceMultiplier = Config.Bind("Inventory", "Extra Slot Price Multiplier", 1.5f, "Price multiplier for each additional inventory slot. For example, with a base price of 40 and a multiplier of 1.5, the first extra slot would cost 40, the second would cost 60 (40 * 1.5), the third would cost 90 (60 * 1.5), and so on.");

            EnableShowMonstersHealthBar = Config.Bind("UI", "Enable Show Monsters Health Bar", true, "Enable or disable showing monsters' health bars.");
            EnableShowMonsterDamageIndicator = Config.Bind("UI", "Enable Show Monster Damage Indicator", true, "Enable or disable showing damage indicators on monsters.");
            MonsterHealthBarExtraHeightOffset = Config.Bind("UI", "Monster Health Bar Extra Height Offset", 0.45f, "Additional height offset for monster health bars to prevent overlap with the monster's model.");

            if (EnableInventorySlotUpgrades.Value)
            {
                ArtisanText.Add(SystemLanguage.English, "inventory_upgrade_full", "Backpack fully upgraded");
                ArtisanText.Add(SystemLanguage.English, "inventory_upgrade_buy", "(Hold) Upgrade backpack ({0} Gold)");
                ArtisanText.Add(SystemLanguage.Portuguese, "inventory_upgrade_full", "Mochila totalmente melhorada");
                ArtisanText.Add(SystemLanguage.Portuguese, "inventory_upgrade_buy", "(Segurar) Melhorar mochila ({0} Ouros)");
            }

            harmony = new Harmony("gamedroit.artisan");

            ArtisanInventoryUpgradeNetwork.Register();

            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                foreach (MethodBase method in harmony.GetPatchedMethods())
                {
                    Logger.LogInfo($"Patched: {method.DeclaringType?.FullName}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Harmony patch failed: {ex}");
            }

            Logger.LogInfo("Artisan mod is loaded and initialized.");
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
    }
}